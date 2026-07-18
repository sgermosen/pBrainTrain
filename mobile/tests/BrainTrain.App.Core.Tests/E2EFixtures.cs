using BrainTrain.App.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BrainTrain.App.Core.Tests;

/// <summary>Backend BrainTrain real, en memoria, con SQLite temporal y seed completo.</summary>
public class BackendFixture : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"braintrain-m-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}",
            ["Database:Provider"] = "sqlite",
            ["Game:MinSessionSeconds"] = "0",
            ["RateLimiting:GlobalPerMinute"] = "100000",
            ["RateLimiting:AuthPerMinute"] = "100000",
            ["Store:AllowTestReceipts"] = "true"
        }));
    }

    /// <summary>ApiClient de la app apuntando al backend en memoria.</summary>
    public ApiClient NewApiClient(out ITokenStore store)
    {
        store = new InMemoryTokenStore();
        return new ApiClient(CreateClient(), store);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
}

// ----------------------------- dobles de prueba -----------------------------

public sealed class FakeNav : INavigationService
{
    public List<string> Routes { get; } = [];
    public Task GoToAsync(string route) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoBackAsync() { Routes.Add("<back>"); return Task.CompletedTask; }
}

public sealed class FakeDevice : IDeviceIdentity
{
    private readonly string _id = $"test-device-{Guid.NewGuid():N}";
    public Task<string> GetDeviceIdAsync() => Task.FromResult(_id);
}

public sealed class FakeReminders : IReminderScheduler
{
    public TimeSpan? Scheduled { get; private set; }
    public bool Cancelled { get; private set; }
    public Task ScheduleDailyAsync(TimeSpan localTime, string title, string message)
    { Scheduled = localTime; return Task.CompletedTask; }
    public Task CancelAllAsync() { Cancelled = true; Scheduled = null; return Task.CompletedTask; }
    public Task<bool> RequestPermissionAsync() => Task.FromResult(true);
}

/// <summary>Compra sandbox: genera un recibo de prueba que el backend acepta en dev.</summary>
public sealed class FakePurchaser : IPlatformPurchaser
{
    public Task<PlatformPurchase?> BuyAsync(string productId, CancellationToken ct = default) =>
        Task.FromResult<PlatformPurchase?>(new PlatformPurchase("Test", $"tx-{Guid.NewGuid():N}", "TEST-OK"));
}

/// <summary>Anuncios simulados: el rewarded siempre se "completa".</summary>
public sealed class FakeAds : IAdService
{
    public bool BannerEnabled => false;
    public int RewardedShown { get; private set; }
    public Task<bool> ShowRewardedAdAsync(CancellationToken ct = default)
    { RewardedShown++; return Task.FromResult(true); }
    public Task ShowInterstitialIfDueAsync(CancellationToken ct = default) => Task.CompletedTask;
}
