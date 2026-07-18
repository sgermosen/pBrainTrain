using BrainTrain.App.Core;
using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Core.Tests;

/// <summary>
/// End-to-end de verdad: los ViewModels de la app MAUI ejecutando sus comandos
/// contra el backend real (en memoria, con el contenido sembrado completo).
/// </summary>
public class ViewModelE2ETests : IClassFixture<BackendFixture>
{
    private readonly BackendFixture _backend;

    public ViewModelE2ETests(BackendFixture backend) => _backend = backend;

    private async Task<(ApiClient Api, FakeNav Nav)> LoggedGuestAsync()
    {
        var api = _backend.NewApiClient(out _);
        var nav = new FakeNav();
        var onboarding = new OnboardingViewModel(api, new FakeDevice(), nav) { DisplayName = "Testi" };
        await onboarding.PlayAsGuestCommand.ExecuteAsync(null);
        Assert.Contains("//home", nav.Routes);
        return (api, nav);
    }

    [Fact]
    public async Task Onboarding_InvitadoEntraDirectoAlJuego()
    {
        var (api, _) = await LoggedGuestAsync();
        var profile = await api.GetProfileAsync();
        Assert.Equal("Testi", profile.DisplayName);
        Assert.True(profile.IsGuest);
    }

    [Fact]
    public async Task Home_CargaPerfilRetoYCategorias()
    {
        var (api, nav) = await LoggedGuestAsync();
        var home = new HomeViewModel(api, new GameFlow(), nav);
        await home.LoadCommand.ExecuteAsync(null);

        Assert.NotNull(home.Profile);
        Assert.Equal(6, home.Categories.Count);
        Assert.False(home.DailyCompleted);
        Assert.Equal("5/5", home.LivesLabel);
    }

    [Fact]
    public async Task PartidaCompleta_DesdeHomeHastaResultados()
    {
        var (api, nav) = await LoggedGuestAsync();
        var flow = new GameFlow();
        var home = new HomeViewModel(api, flow, nav);
        await home.LoadCommand.ExecuteAsync(null);

        await home.PlayQuickCommand.ExecuteAsync(null);
        Assert.Contains("quiz", nav.Routes);
        Assert.NotNull(flow.CurrentGame);
        Assert.Equal(7, flow.CurrentGame!.Questions.Count);

        // El quiz responde todas (elige siempre la primera opción).
        var quiz = new QuizViewModel(api, flow, nav);
        quiz.Start();
        for (var i = 0; i < 7; i++)
        {
            quiz.SelectChoiceCommand.Execute(quiz.Choices[0]);
            await quiz.ConfirmCommand.ExecuteAsync(null);
        }
        quiz.Dispose();

        Assert.Contains("results", nav.Routes);
        Assert.NotNull(flow.LastResult);

        // La pantalla de resultados arma el repaso didáctico con explicaciones.
        var results = new ResultsViewModel(flow, nav);
        results.Load();
        Assert.Equal(7, results.Review.Count);
        Assert.All(results.Review, r => Assert.False(string.IsNullOrEmpty(r.Explanation)));
        Assert.NotEmpty(results.Headline);
        // Primera partida completada → logro desbloqueado visible en la celebración.
        Assert.Contains(results.Unlocked, a => a.Code == "primera_partida");
    }

    [Fact]
    public async Task SinVidas_LaAppTeLlevaALaTienda_YLaCompraSandboxResuelve()
    {
        var (api, nav) = await LoggedGuestAsync();
        var flow = new GameFlow();
        var home = new HomeViewModel(api, flow, nav);

        // Quema las 5 vidas.
        for (var i = 0; i < 5; i++)
            await home.PlayQuickCommand.ExecuteAsync(null);

        nav.Routes.Clear();
        await home.PlayQuickCommand.ExecuteAsync(null);
        Assert.Contains("store", nav.Routes); // sin vidas → tienda
        Assert.NotNull(home.Error);

        // Compra en la tienda (flujo sandbox completo: nativo fake + canje verificado).
        var store = new StoreViewModel(api, new FakePurchaser(), new FakeAds());
        await store.LoadCommand.ExecuteAsync(null);
        var refill = store.Products.First(p => p.ProductId == "braintrain.lives.refill");
        await store.BuyCommand.ExecuteAsync(refill);

        Assert.Null(store.Error);
        Assert.NotNull(store.Message);
        Assert.True(store.Profile!.Lives.Current >= 5);
    }

    [Fact]
    public async Task RetoDiario_CuentaYSeMarcaCompletado()
    {
        var (api, nav) = await LoggedGuestAsync();
        var flow = new GameFlow();
        var home = new HomeViewModel(api, flow, nav);
        await home.LoadCommand.ExecuteAsync(null);

        await home.PlayDailyCommand.ExecuteAsync(null);
        Assert.NotNull(flow.CurrentGame);

        var quiz = new QuizViewModel(api, flow, nav);
        quiz.Start();
        for (var i = 0; i < flow.CurrentGame!.Questions.Count; i++)
        {
            quiz.SelectChoiceCommand.Execute(quiz.Choices[0]);
            await quiz.ConfirmCommand.ExecuteAsync(null);
        }
        quiz.Dispose();

        await home.LoadCommand.ExecuteAsync(null);
        Assert.True(home.DailyCompleted);
        Assert.Equal(1, home.Profile!.Streak.Current);
    }

    [Fact]
    public async Task Logros_YLeaderboard_SeCargan()
    {
        var (api, _) = await LoggedGuestAsync();

        var achievements = new AchievementsViewModel(api);
        await achievements.LoadCommand.ExecuteAsync(null);
        Assert.True(achievements.TotalCount >= 20);

        var board = new LeaderboardViewModel(api);
        await board.LoadCommand.ExecuteAsync(null);
        Assert.Null(board.Error);
    }

    [Fact]
    public async Task Ajustes_ProgramaRecordatorioYAsciendeCuenta()
    {
        var (api, nav) = await LoggedGuestAsync();
        var reminders = new FakeReminders();
        var settings = new SettingsViewModel(api, reminders, new InMemoryPreferences(), nav);
        await settings.LoadCommand.ExecuteAsync(null);

        settings.RemindersEnabled = true;
        settings.ReminderTime = new TimeSpan(20, 30, 0);
        await settings.ApplyRemindersCommand.ExecuteAsync(null);
        Assert.Equal(new TimeSpan(20, 30, 0), reminders.Scheduled);

        // Invitado → cuenta completa sin perder el progreso.
        var auth = new AuthViewModel(api, nav)
        {
            Mode = "upgrade",
            Email = $"upgrade{Guid.NewGuid():N}@test.com",
            Password = "clave-muy-segura-1"
        };
        await auth.SubmitCommand.ExecuteAsync(null);
        Assert.Null(auth.Error);
        var profile = await api.GetProfileAsync();
        Assert.False(profile.IsGuest);
    }

    [Fact]
    public async Task Entrenamiento_CargaMinijuegosYNavega()
    {
        var (api, nav) = await LoggedGuestAsync();
        var training = new TrainingViewModel(api, nav);
        await training.LoadCommand.ExecuteAsync(null);

        Assert.Equal(10, training.Games.Count);
        await training.OpenCommand.ExecuteAsync(training.Games.First(g => g.Code == "g2048"));
        Assert.Contains("game2048", nav.Routes);
        await training.OpenCommand.ExecuteAsync(training.Games.First(g => g.Code == "rubik_guide"));
        Assert.Contains("rubikguide", nav.Routes);
    }

    [Fact]
    public async Task VerAnuncio_DaUnaVidaViaTienda()
    {
        var (api, nav) = await LoggedGuestAsync();
        var flow = new GameFlow();
        var home = new HomeViewModel(api, flow, nav);
        await home.PlayQuickCommand.ExecuteAsync(null); // gasta una vida

        var ads = new FakeAds();
        var store = new StoreViewModel(api, new FakePurchaser(), ads);
        await store.LoadCommand.ExecuteAsync(null);
        await store.WatchAdForLifeCommand.ExecuteAsync(null);

        Assert.Equal(1, ads.RewardedShown);
        Assert.Null(store.Error);
        Assert.Equal(5, store.Profile!.Lives.Current);
        Assert.Equal(4, store.AdLivesRemaining);
    }

    [Fact]
    public async Task Premium_SeCompraYSeReflejaEnElPerfil()
    {
        var (api, _) = await LoggedGuestAsync();
        var store = new StoreViewModel(api, new FakePurchaser(), new FakeAds());
        await store.LoadCommand.ExecuteAsync(null);

        var premium = store.Products.First(p => p.ProductId == "braintrain.premium.month");
        await store.BuyCommand.ExecuteAsync(premium);

        Assert.Null(store.Error);
        Assert.True(store.Profile!.IsPremium);
        Assert.False(store.Profile.ShowAds);
        Assert.Equal(8, store.Profile.Lives.Max);
    }

    [Fact]
    public async Task TokenExpirado_SeRefrescaSolo()
    {
        var api = _backend.NewApiClient(out var store);
        var nav = new FakeNav();
        var onboarding = new OnboardingViewModel(api, new FakeDevice(), nav);
        await onboarding.PlayAsGuestCommand.ExecuteAsync(null);

        // Simula access token corrupto/expirado: la siguiente llamada debe
        // refrescar con el refresh token y completarse sin error.
        var (_, refresh) = await store.LoadAsync();
        Assert.NotNull(refresh);
        var apiField = typeof(ApiClient).GetField("_accessToken",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        apiField.SetValue(api, "token-invalido");

        var profile = await api.GetProfileAsync();
        Assert.NotNull(profile);
    }
}
