using BrainTrain.App.Core;

namespace BrainTrain.App.Services;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));

    public Task GoBackAsync() =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));
}

/// <summary>Tokens en el almacén cifrado del sistema (Keystore/Keychain).</summary>
public sealed class SecureStorageTokenStore : ITokenStore
{
    private const string AccessKey = "bt.access";
    private const string RefreshKey = "bt.refresh";

    public async Task<(string?, string?)> LoadAsync() =>
        (await SecureStorage.GetAsync(AccessKey), await SecureStorage.GetAsync(RefreshKey));

    public async Task SaveAsync(string access, string refresh)
    {
        await SecureStorage.SetAsync(AccessKey, access);
        await SecureStorage.SetAsync(RefreshKey, refresh);
    }

    public Task ClearAsync()
    {
        SecureStorage.Remove(AccessKey);
        SecureStorage.Remove(RefreshKey);
        return Task.CompletedTask;
    }
}

public sealed class MauiPreferences : IAppPreferences
{
    public string? Get(string key, string? fallback = null) => Preferences.Get(key, fallback);
    public void Set(string key, string? value)
    {
        if (value is null) Preferences.Remove(key);
        else Preferences.Set(key, value);
    }
}

/// <summary>Identificador estable por instalación para la cuenta de invitado.</summary>
public sealed class DeviceIdentity : IDeviceIdentity
{
    private const string Key = "bt.deviceid";

    public Task<string> GetDeviceIdAsync()
    {
        var id = Preferences.Get(Key, null);
        if (string.IsNullOrEmpty(id))
        {
            id = $"maui-{Guid.NewGuid():N}";
            Preferences.Set(Key, id);
        }
        return Task.FromResult(id);
    }
}

/// <summary>Plataformas sin recordatorios locales implementados.</summary>
public sealed class NoopReminderScheduler : IReminderScheduler
{
    public Task ScheduleDailyAsync(TimeSpan localTime, string title, string message) => Task.CompletedTask;
    public Task CancelAllAsync() => Task.CompletedTask;
    public Task<bool> RequestPermissionAsync() => Task.FromResult(false);
}

/// <summary>
/// Compra sandbox para desarrollo: el backend (con Store:AllowTestReceipts=true)
/// acepta el recibo "TEST-OK". En producción se reemplaza por Google Play
/// Billing / StoreKit — pasos exactos en docs/PUBLICACION.md.
/// </summary>
public sealed class SandboxPurchaser : IPlatformPurchaser
{
    public Task<PlatformPurchase?> BuyAsync(string productId, CancellationToken ct = default) =>
        Task.FromResult<PlatformPurchase?>(
            new PlatformPurchase("Test", $"sandbox-{Guid.NewGuid():N}", "TEST-OK"));
}
