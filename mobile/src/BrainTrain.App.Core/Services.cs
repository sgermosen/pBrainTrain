namespace BrainTrain.App.Core;

/// <summary>Navegación abstraída de MAUI Shell para poder testear ViewModels.</summary>
public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoBackAsync();
}

/// <summary>Recordatorios locales (notificaciones) — pieza clave del hábito diario.</summary>
public interface IReminderScheduler
{
    /// <summary>Programa el recordatorio diario del reto a la hora local indicada.</summary>
    Task ScheduleDailyAsync(TimeSpan localTime, string title, string message);
    Task CancelAllAsync();
    Task<bool> RequestPermissionAsync();
}

/// <summary>Resultado de una compra nativa (Google Play / App Store).</summary>
public sealed record PlatformPurchase(string Platform, string TransactionId, string Receipt);

/// <summary>
/// Abstracción del billing nativo. La implementación de producción usa
/// Google Play Billing / StoreKit; en desarrollo se usa el sandbox del backend.
/// </summary>
public interface IPlatformPurchaser
{
    Task<PlatformPurchase?> BuyAsync(string productId, CancellationToken ct = default);
}

/// <summary>
/// Anuncios de la plataforma (AdMob). El rewarded devuelve true solo si el
/// usuario completó el anuncio; el servidor otorga la recompensa con tope diario.
/// </summary>
public interface IAdService
{
    bool BannerEnabled { get; }
    Task<bool> ShowRewardedAdAsync(CancellationToken ct = default);
    Task ShowInterstitialIfDueAsync(CancellationToken ct = default);
}

/// <summary>Reproductor de los audios locales de la sección Enfoque (loops + campana).</summary>
public interface IFocusAudioPlayer
{
    Task StartLoopAsync(string assetName);
    Task StopAsync();
    Task PlayChimeAsync();
}

public sealed class NoopFocusAudioPlayer : IFocusAudioPlayer
{
    public Task StartLoopAsync(string assetName) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public Task PlayChimeAsync() => Task.CompletedTask;
}

/// <summary>Identidad estable del dispositivo para cuentas de invitado.</summary>
public interface IDeviceIdentity
{
    Task<string> GetDeviceIdAsync();
}

/// <summary>
/// Estado compartido de la partida en curso entre Quiz y Results.
/// Singleton simple: solo hay una partida activa a la vez.
/// </summary>
public sealed class GameFlow
{
    public StartGameResponse? CurrentGame { get; set; }
    public string Mode { get; set; } = GameModes.Quick;
    public CategoryDto? Category { get; set; }
    public GameResultDto? LastResult { get; set; }
}

/// <summary>Preferencias simples (envuelve MAUI Preferences; diccionario en tests).</summary>
public interface IAppPreferences
{
    string? Get(string key, string? fallback = null);
    void Set(string key, string? value);
}

public sealed class InMemoryPreferences : IAppPreferences
{
    private readonly Dictionary<string, string?> _data = [];
    public string? Get(string key, string? fallback = null) => _data.GetValueOrDefault(key, fallback);
    public void Set(string key, string? value) => _data[key] = value;
}
