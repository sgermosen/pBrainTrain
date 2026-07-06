using BrainTrain.App.Core;
using BrainTrain.App.Core.ViewModels;
using BrainTrain.App.Pages;
using BrainTrain.App.Services;
using Microsoft.Extensions.Logging;

namespace BrainTrain.App;

public static class MauiProgram
{
    /// <summary>
    /// URL base de la API. En Android el emulador ve la máquina host como 10.0.2.2.
    /// En Release apunta al dominio de producción (configurar antes de publicar).
    /// </summary>
#if DEBUG
    public const string ApiBaseUrl = "http://10.0.2.2:5116/";
#else
    public const string ApiBaseUrl = "https://api.tudominio.com/"; // TODO: dominio real antes de publicar
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var services = builder.Services;

        // ---------- Núcleo ----------
        services.AddSingleton<ITokenStore, SecureStorageTokenStore>();
        services.AddSingleton(sp => new ApiClient(
            new HttpClient { BaseAddress = new Uri(ApiBaseUrl), Timeout = TimeSpan.FromSeconds(20) },
            sp.GetRequiredService<ITokenStore>()));
        services.AddSingleton<GameFlow>();
        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IAppPreferences, MauiPreferences>();
        services.AddSingleton<IDeviceIdentity, DeviceIdentity>();
#if ANDROID
        services.AddSingleton<IReminderScheduler, BrainTrain.App.Platforms.Android.AndroidReminderScheduler>();
#else
        services.AddSingleton<IReminderScheduler, NoopReminderScheduler>();
#endif
        // Compras: sandbox en DEBUG (backend con AllowTestReceipts). Para producción,
        // sustituir por la integración de Google Play Billing / StoreKit (ver PUBLICACION.md).
        services.AddSingleton<IPlatformPurchaser, SandboxPurchaser>();

        // ---------- ViewModels ----------
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<AuthViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<QuizViewModel>();
        services.AddTransient<ResultsViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<AchievementsViewModel>();
        services.AddTransient<LeaderboardViewModel>();
        services.AddTransient<StoreViewModel>();
        services.AddTransient<SettingsViewModel>();

        // ---------- Páginas ----------
        services.AddTransient<OnboardingPage>();
        services.AddTransient<AuthPage>();
        services.AddTransient<HomePage>();
        services.AddTransient<QuizPage>();
        services.AddTransient<ResultsPage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<AchievementsPage>();
        services.AddTransient<LeaderboardPage>();
        services.AddTransient<StorePage>();
        services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
