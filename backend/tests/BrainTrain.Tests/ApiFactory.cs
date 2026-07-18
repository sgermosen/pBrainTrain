using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BrainTrain.Api;
using BrainTrain.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BrainTrain.Tests;

/// <summary>
/// Levanta la API completa (seed incluido) contra una base SQLite temporal única.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"braintrain-test-{Guid.NewGuid():N}.db");

    /// <summary>Overrides adicionales de configuración (subclases de fixture).</summary>
    protected virtual Dictionary<string, string?> ExtraSettings => [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}",
            ["Database:Provider"] = "sqlite",
            ["Database:SeedOnStartup"] = "true",
            ["Game:MinSessionSeconds"] = "0",
            ["RateLimiting:GlobalPerMinute"] = "100000",
            ["RateLimiting:AuthPerMinute"] = "100000",
            ["Store:AllowTestReceipts"] = "true",
            // PayPal "configurado" con credenciales falsas: el gateway real se
            // sustituye por FakePayPalGateway más abajo.
            ["PayPal:ClientId"] = "test-client-id",
            ["PayPal:Secret"] = "test-secret",
            ["Admin:Key"] = "test-admin-key-1234567890"
        };
        foreach (var (k, v) in ExtraSettings) settings[k] = v;
        builder.UseSetting("environment", "Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(settings));
        builder.ConfigureServices(s =>
            s.AddSingleton<IPayPalGateway>(new FakePayPalGateway()));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
}

/// <summary>Gateway PayPal simulado: órdenes en memoria, capturas siempre completadas.</summary>
public sealed class FakePayPalGateway : IPayPalGateway
{
    private readonly Dictionary<string, (decimal Amount, string Currency, string CustomId)> _orders = [];
    private int _seq;

    public Task<string> CreateOrderAsync(decimal amount, string currency, string description, string customId, CancellationToken ct)
    {
        var id = $"PPORDER-{Interlocked.Increment(ref _seq)}";
        lock (_orders) _orders[id] = (amount, currency, customId);
        return Task.FromResult(id);
    }

    public Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken ct)
    {
        lock (_orders)
        {
            if (!_orders.TryGetValue(orderId, out var o))
                return Task.FromResult(new PayPalCaptureResult(false, "", null, 0, ""));
            _orders.Remove(orderId); // una orden solo se captura una vez
            return Task.FromResult(new PayPalCaptureResult(true, $"PPCAP-{Guid.NewGuid():N}", o.CustomId, o.Amount, o.Currency));
        }
    }
}

/// <summary>Variante con solo 2 vidas máximas para probar el agotamiento rápido.</summary>
public class SmallLivesApiFactory : ApiFactory
{
    protected override Dictionary<string, string?> ExtraSettings =>
        new() { ["Game:MaxLives"] = "2" };
}

public static class TestClientExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static async Task<T> ReadAs<T>(this HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        Assert.True(resp.IsSuccessStatusCode, $"HTTP {(int)resp.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, Json)!;
    }

    public static Task<HttpResponseMessage> PostJson<T>(this HttpClient client, string url, T body) =>
        client.PostAsync(url, JsonContent.Create(body, options: Json));

    /// <summary>Crea una cuenta de invitado nueva y deja el token puesto en el cliente.</summary>
    public static async Task<AuthResponse> NewGuestAsync(this HttpClient client, string? name = null)
    {
        var resp = await client.PostJson("/api/v1/auth/guest",
            new GuestRequest($"device-{Guid.NewGuid():N}", name));
        var auth = await resp.ReadAs<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }
}
