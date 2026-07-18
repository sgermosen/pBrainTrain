using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrainTrain.App.Core;

public sealed class ApiException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

/// <summary>Persistencia de tokens (SecureStorage en la app; memoria en tests).</summary>
public interface ITokenStore
{
    Task<(string? Access, string? Refresh)> LoadAsync();
    Task SaveAsync(string access, string refresh);
    Task ClearAsync();
}

public sealed class InMemoryTokenStore : ITokenStore
{
    private string? _access, _refresh;
    public Task<(string?, string?)> LoadAsync() => Task.FromResult((_access, _refresh));
    public Task SaveAsync(string access, string refresh) { _access = access; _refresh = refresh; return Task.CompletedTask; }
    public Task ClearAsync() { _access = _refresh = null; return Task.CompletedTask; }
}

/// <summary>
/// Cliente tipado de la BrainTrain API. Maneja el Bearer token y reintenta
/// una vez con refresh automático cuando el access token expira.
/// </summary>
public sealed class ApiClient(HttpClient http, ITokenStore tokens)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private string? _accessToken;

    public bool HasSession { get; private set; }

    public async Task InitializeAsync()
    {
        var (access, _) = await tokens.LoadAsync();
        _accessToken = access;
        HasSession = access is not null;
    }

    // ---------------- Auth ----------------
    public Task<AuthResponse> GuestLoginAsync(string deviceId, string? name, CancellationToken ct = default) =>
        AuthCallAsync("api/v1/auth/guest", new GuestRequest(deviceId, name), ct);

    public Task<AuthResponse> RegisterAsync(string email, string password, string name, CancellationToken ct = default) =>
        AuthCallAsync("api/v1/auth/register", new RegisterRequest(email, password, name), ct);

    public Task<AuthResponse> LoginAsync(string email, string password, CancellationToken ct = default) =>
        AuthCallAsync("api/v1/auth/login", new LoginRequest(email, password), ct);

    public Task<AuthResponse> UpgradeAsync(string email, string password, CancellationToken ct = default) =>
        AuthCallAsync("api/v1/auth/upgrade", new UpgradeRequest(email, password), ct, authorized: true);

    public async Task LogoutAsync()
    {
        _accessToken = null;
        HasSession = false;
        await tokens.ClearAsync();
    }

    private async Task<AuthResponse> AuthCallAsync<TReq>(string url, TReq body, CancellationToken ct, bool authorized = false)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: Json)
        };
        if (authorized && _accessToken is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        using var resp = await http.SendAsync(req, ct);
        var auth = await ReadAsync<AuthResponse>(resp, ct);
        _accessToken = auth.AccessToken;
        HasSession = true;
        await tokens.SaveAsync(auth.AccessToken, auth.RefreshToken);
        return auth;
    }

    // ---------------- Juego ----------------
    public Task<ProfileDto> GetProfileAsync(CancellationToken ct = default) =>
        GetAsync<ProfileDto>("api/v1/me", ct);

    public Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequest req, CancellationToken ct = default) =>
        SendAsync<ProfileDto>(new HttpMethod("PATCH"), "api/v1/me", req, ct);

    public Task<LivesDto> GetLivesAsync(CancellationToken ct = default) =>
        GetAsync<LivesDto>("api/v1/me/lives", ct);

    public Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default) =>
        GetAsync<List<CategoryDto>>("api/v1/categories", ct);

    public Task<StartGameResponse> StartGameAsync(string mode, int? categoryId, CancellationToken ct = default) =>
        SendAsync<StartGameResponse>(HttpMethod.Post, "api/v1/game/start", new StartGameRequest(mode, categoryId), ct);

    public Task<GameResultDto> SubmitGameAsync(Guid sessionId, List<SubmitAnswer> answers, CancellationToken ct = default) =>
        SendAsync<GameResultDto>(HttpMethod.Post, $"api/v1/game/{sessionId}/submit", new SubmitGameRequest(answers), ct);

    public Task<DailyStatusDto> GetDailyStatusAsync(CancellationToken ct = default) =>
        GetAsync<DailyStatusDto>("api/v1/daily", ct);

    public Task<List<AchievementDto>> GetAchievementsAsync(CancellationToken ct = default) =>
        GetAsync<List<AchievementDto>>("api/v1/me/achievements", ct);

    public Task<LeaderboardDto> GetLeaderboardAsync(CancellationToken ct = default) =>
        GetAsync<LeaderboardDto>("api/v1/leaderboard/weekly", ct);

    public Task<StoreCatalogDto> GetStoreCatalogAsync(CancellationToken ct = default) =>
        GetAsync<StoreCatalogDto>("api/v1/store/catalog", ct);

    public Task<PurchaseResponse> PurchaseAsync(PurchaseRequest req, CancellationToken ct = default) =>
        SendAsync<PurchaseResponse>(HttpMethod.Post, "api/v1/store/purchase", req, ct);

    public Task<ProfileDto> RefillWithCoinsAsync(CancellationToken ct = default) =>
        SendAsync<ProfileDto>(HttpMethod.Post, "api/v1/store/refill-with-coins", new { }, ct);

    public Task<AdRewardResponse> ClaimAdRewardAsync(CancellationToken ct = default) =>
        SendAsync<AdRewardResponse>(HttpMethod.Post, "api/v1/ads/reward-life", new { }, ct);

    public Task<List<MinigameDto>> GetMinigamesAsync(CancellationToken ct = default) =>
        GetAsync<List<MinigameDto>>("api/v1/minigames", ct);

    public Task<MinigameResultDto> SubmitMinigameAsync(string code, int score, int durationMs, CancellationToken ct = default) =>
        SendAsync<MinigameResultDto>(HttpMethod.Post, "api/v1/minigames/submit",
            new MinigameSubmitRequest(code, score, durationMs), ct);

    public Task RegisterDeviceAsync(string platform, string token, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, "api/v1/me/devices", new DeviceTokenRequest(platform, token), ct, allowEmpty: true);

    // ---------------- Interno ----------------
    private Task<T> GetAsync<T>(string url, CancellationToken ct) =>
        SendAsync<T>(HttpMethod.Get, url, (object?)null, ct)!;

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct, bool allowEmpty = false)
    {
        var resp = await SendAuthorizedAsync(method, url, body, ct);

        // Access token expirado: refresca una vez y reintenta.
        if (resp.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync(ct))
        {
            resp.Dispose();
            resp = await SendAuthorizedAsync(method, url, body, ct);
        }

        using (resp)
        {
            if (allowEmpty && resp.IsSuccessStatusCode)
                return default!;
            return await ReadAsync<T>(resp, ct);
        }
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, url);
        if (body is not null)
            req.Content = JsonContent.Create(body, options: Json);
        if (_accessToken is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await http.SendAsync(req, ct);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        var (_, refresh) = await tokens.LoadAsync();
        if (refresh is null) return false;
        try
        {
            using var resp = await http.PostAsync("api/v1/auth/refresh",
                JsonContent.Create(new RefreshRequest(refresh), options: Json), ct);
            if (!resp.IsSuccessStatusCode) return false;
            var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>(Json, ct))!;
            _accessToken = auth.AccessToken;
            await tokens.SaveAsync(auth.AccessToken, auth.RefreshToken);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<T>(Json, ct))!;

        var code = "error";
        var message = $"Error {(int)resp.StatusCode}";
        try
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemPayload>(Json, ct);
            if (problem is not null)
            {
                code = problem.Title ?? code;
                message = problem.Detail ?? message;
            }
        }
        catch (JsonException) { /* cuerpo no-JSON: usa el mensaje genérico */ }

        throw new ApiException((int)resp.StatusCode, code, message);
    }

    private sealed record ProblemPayload(string? Title, string? Detail);
}
