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

/// <summary>Hoja de compartir nativa del sistema.</summary>
public sealed class MauiShareService : IShareService
{
    public Task ShareTextAsync(string title, string text) =>
        Share.Default.RequestAsync(new ShareTextRequest { Title = title, Text = text });
}

/// <summary>Vibración de feedback; silenciosa si el dispositivo no la soporta.</summary>
public sealed class MauiHaptics : IHaptics
{
    public void Click()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch (FeatureNotSupportedException) { }
    }

    public void Success()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); }
        catch (FeatureNotSupportedException) { }
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
/// Anuncios simulados para desarrollo: "muestra" el rewarded con una pequeña
/// espera y devuelve completado. En producción se reemplaza por AdMob
/// (Plugin.MauiMTAdmob) — pasos exactos en docs/PUBLICACION.md.
/// </summary>
public sealed class SandboxAdService : IAdService
{
    public bool BannerEnabled => false;

    public async Task<bool> ShowRewardedAdAsync(CancellationToken ct = default)
    {
        await Task.Delay(1200, ct); // simula la duración del anuncio
        return true;
    }

    public Task ShowInterstitialIfDueAsync(CancellationToken ct = default) => Task.CompletedTask;
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
