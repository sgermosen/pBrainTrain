using System.Net.Http.Headers;
using System.Text.Json;
using BrainTrain.Domain;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

public sealed record PayPalCaptureResult(bool Completed, string CaptureId, string? CustomId, decimal Amount, string Currency);

/// <summary>
/// Puerta a la API REST de PayPal (Orders v2). Abstraída para poder simularla
/// en pruebas sin tocar la red.
/// </summary>
public interface IPayPalGateway
{
    Task<string> CreateOrderAsync(decimal amount, string currency, string description, string customId, CancellationToken ct);
    Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken ct);
}

/// <summary>Implementación real contra api-m(.sandbox).paypal.com con OAuth client-credentials.</summary>
public sealed class HttpPayPalGateway(HttpClient http, IOptions<PayPalOptions> options) : IPayPalGateway
{
    private readonly PayPalOptions _opt = options.Value;
    private string? _token;
    private DateTime _tokenExpiresUtc;
    private readonly SemaphoreSlim _tokenGate = new(1, 1);

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token is not null && _tokenExpiresUtc > DateTime.UtcNow.AddMinutes(1))
            return _token;

        await _tokenGate.WaitAsync(ct);
        try
        {
            if (_token is not null && _tokenExpiresUtc > DateTime.UtcNow.AddMinutes(1))
                return _token;

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_opt.BaseUrl}/v1/oauth2/token")
            {
                Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "client_credentials")])
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_opt.ClientId}:{_opt.Secret}")));

            using var resp = await http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            _token = doc.RootElement.GetProperty("access_token").GetString()!;
            _tokenExpiresUtc = DateTime.UtcNow.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32());
            return _token;
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    public async Task<string> CreateOrderAsync(decimal amount, string currency, string description, string customId, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        var body = JsonSerializer.Serialize(new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    description,
                    custom_id = customId,
                    amount = new { currency_code = currency, value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) }
                }
            }
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_opt.BaseUrl}/v2/checkout/orders")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_opt.BaseUrl}/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            return new PayPalCaptureResult(false, "", null, 0, "");

        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("status").GetString();
        var capture = doc.RootElement.GetProperty("purchase_units")[0]
            .GetProperty("payments").GetProperty("captures")[0];
        var amount = capture.GetProperty("amount");

        return new PayPalCaptureResult(
            Completed: status == "COMPLETED",
            CaptureId: capture.GetProperty("id").GetString()!,
            CustomId: capture.TryGetProperty("custom_id", out var cid) ? cid.GetString() : null,
            Amount: decimal.Parse(amount.GetProperty("value").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
            Currency: amount.GetProperty("currency_code").GetString()!);
    }
}

/// <summary>
/// Flujo de compra vía PayPal para el portal web: crear orden → aprobar en la
/// UI de PayPal → capturar y acreditar en el servidor (nunca se confía en el cliente).
/// </summary>
public sealed class PayPalCheckoutService(
    IPayPalGateway gateway,
    StoreService store,
    IOptions<PayPalOptions> options)
{
    private readonly PayPalOptions _opt = options.Value;

    public PayPalConfigDto Config() => new(_opt.IsConfigured, _opt.ClientId, _opt.Currency, _opt.Mode);

    public async Task<PayPalCreateOrderResponse> CreateOrderAsync(long userId, string productId, CancellationToken ct)
    {
        EnsureConfigured();
        var product = store.FindProduct(productId)
                      ?? throw new GameError(400, "unknown_product", "Producto desconocido.");
        if (product.PriceUsd <= 0)
            throw new GameError(400, "not_sold_via_paypal", "Este producto no está disponible vía PayPal.");

        var orderId = await gateway.CreateOrderAsync(
            product.PriceUsd, _opt.Currency, $"BrainTrain — {product.Name}",
            customId: $"{productId}|{userId}", ct);
        return new PayPalCreateOrderResponse(orderId);
    }

    public async Task<PurchaseResponse> CaptureAsync(long userId, string orderId, CancellationToken ct)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(orderId) || orderId.Length > 64)
            throw new GameError(400, "bad_order", "Orden inválida.");

        var result = await gateway.CaptureOrderAsync(orderId, ct);
        if (!result.Completed)
            throw new GameError(402, "paypal_not_completed", "El pago no se completó.");

        // El custom_id viene de NUESTRA creación de orden vía la API de PayPal (no del cliente).
        var parts = (result.CustomId ?? "").Split('|');
        var product = parts.Length == 2 ? store.FindProduct(parts[0]) : null;
        if (product is null || !long.TryParse(parts[1], out var orderUserId))
            throw new GameError(400, "unknown_product", "La orden no corresponde a un producto válido.");
        if (orderUserId != userId)
            throw new GameError(403, "wrong_user", "La orden pertenece a otra cuenta.");
        if (result.Amount < product.PriceUsd || !result.Currency.Equals(_opt.Currency, StringComparison.OrdinalIgnoreCase))
            throw new GameError(402, "amount_mismatch", "El monto pagado no coincide con el producto.");

        return await store.GrantProductAsync(userId, product, StorePlatform.PayPal,
            transactionId: result.CaptureId, receipt: $"paypal:{orderId}", ct);
    }

    private void EnsureConfigured()
    {
        if (!_opt.IsConfigured)
            throw new GameError(503, "paypal_not_configured",
                "PayPal no está configurado en el servidor (PayPal__ClientId / PayPal__Secret).");
    }
}
