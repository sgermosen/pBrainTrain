using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BrainTrain.Api.Services;

public sealed class PushOptions
{
    public const string Section = "Push";

    /// <summary>JSON completo de la service account de Firebase (variable de entorno Push__ServiceAccountJson).</summary>
    public string ServiceAccountJson { get; set; } = string.Empty;

    /// <summary>Hora UTC a la que se envía el recordatorio de racha en riesgo.</summary>
    public int StreakReminderHourUtc { get; set; } = 23;
    public bool StreakReminderEnabled { get; set; }
}

public interface IPushSender
{
    bool IsConfigured { get; }
    /// <summary>Envía una notificación; false si el token es inválido (para depurarlo).</summary>
    Task<bool> SendAsync(string deviceToken, string title, string body, CancellationToken ct);
}

/// <summary>
/// Firebase Cloud Messaging HTTP v1: OAuth con la service account (JWT RS256
/// intercambiado por access token) y envío por token de dispositivo.
/// </summary>
public sealed class FcmPushSender : IPushSender
{
    private readonly HttpClient _http;
    private readonly string? _clientEmail;
    private readonly string? _privateKeyPem;
    private readonly string? _projectId;
    private readonly string _tokenUri = "https://oauth2.googleapis.com/token";
    private string? _accessToken;
    private DateTime _accessExpiresUtc;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FcmPushSender(HttpClient http, IOptions<PushOptions> options)
    {
        _http = http;
        var json = options.Value.ServiceAccountJson;
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var doc = JsonDocument.Parse(json);
            _clientEmail = doc.RootElement.GetProperty("client_email").GetString();
            _privateKeyPem = doc.RootElement.GetProperty("private_key").GetString();
            _projectId = doc.RootElement.GetProperty("project_id").GetString();
            if (doc.RootElement.TryGetProperty("token_uri", out var tu) && tu.GetString() is { } uri)
                _tokenUri = uri;
        }
        catch (Exception)
        {
            _clientEmail = null; // JSON inválido → push deshabilitado, no rompe el arranque
        }
    }

    public bool IsConfigured => _clientEmail is not null && _privateKeyPem is not null && _projectId is not null;

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && _accessExpiresUtc > DateTime.UtcNow.AddMinutes(2))
            return _accessToken;

        await _gate.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && _accessExpiresUtc > DateTime.UtcNow.AddMinutes(2))
                return _accessToken;

            using var rsa = RSA.Create();
            rsa.ImportFromPem(_privateKeyPem!);
            var creds = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                issuer: _clientEmail,
                audience: _tokenUri,
                claims: [new Claim("scope", "https://www.googleapis.com/auth/firebase.messaging")],
                notBefore: now,
                expires: now.AddMinutes(50),
                signingCredentials: creds));

            using var resp = await _http.PostAsync(_tokenUri, new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", jwt)
            ]), ct);
            resp.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            _accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            _accessExpiresUtc = now.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32());
            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SendAsync(string deviceToken, string title, string body, CancellationToken ct)
    {
        if (!IsConfigured) return false;
        var token = await GetAccessTokenAsync(ct);
        var payload = JsonSerializer.Serialize(new
        {
            message = new
            {
                token = deviceToken,
                notification = new { title, body }
            }
        });
        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send")
        {
            Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var resp = await _http.SendAsync(req, ct);
        return resp.IsSuccessStatusCode; // 404/410 → token muerto
    }
}

/// <summary>
/// Barrido diario de rachas en riesgo: usuarios con racha activa que ayer no
/// jugaron reciben UN empujón por push. Corre una vez al día a la hora
/// configurada; deshabilitado salvo que Push esté configurado y activado.
/// </summary>
public sealed class StreakReminderService(
    IServiceScopeFactory scopeFactory,
    IPushSender sender,
    IOptions<PushOptions> options,
    ILogger<StreakReminderService> logger) : BackgroundService
{
    private DateOnly _lastRun = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!options.Value.StreakReminderEnabled || !sender.IsConfigured)
            return;

        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            if (now.Hour == options.Value.StreakReminderHourUtc && _lastRun != today)
            {
                _lastRun = today;
                try { await SweepAsync(today, ct); }
                catch (Exception e) { logger.LogError(e, "Fallo el barrido de recordatorios de racha"); }
            }
            await Task.Delay(TimeSpan.FromMinutes(10), ct);
        }
    }

    private async Task SweepAsync(DateOnly today, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var yesterday = today.AddDays(-1);

        var targets = await db.Users.AsNoTracking()
            .Where(u => u.StreakDays > 0 && u.LastActivityDateUtc == yesterday)
            .Join(db.DeviceTokens.AsNoTracking(), u => u.Id, t => t.UserId,
                (u, t) => new { u.StreakDays, t.Token })
            .Take(5000)
            .ToListAsync(ct);

        var sent = 0;
        foreach (var t in targets)
        {
            if (await sender.SendAsync(t.Token,
                    $"🔥 ¡Tu racha de {t.StreakDays} días peligra!",
                    "Un reto diario de 2 minutos y la salvas. ¡Tú puedes!", ct))
                sent++;
        }
        logger.LogInformation("Recordatorios de racha enviados: {Sent}/{Total}", sent, targets.Count);
    }
}
