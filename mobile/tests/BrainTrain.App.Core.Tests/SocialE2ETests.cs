using BrainTrain.App.Core;
using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Core.Tests;

/// <summary>E2E de duelos, misiones, avatares, calibración y compartir contra el backend real.</summary>
public class SocialE2ETests : IClassFixture<BackendFixture>
{
    private readonly BackendFixture _backend;

    public SocialE2ETests(BackendFixture backend) => _backend = backend;

    private async Task<(ApiClient Api, FakeNav Nav)> LoggedGuestAsync(string name = "Testi")
    {
        var api = _backend.NewApiClient(out _);
        var nav = new FakeNav();
        await new OnboardingViewModel(api, new FakeDevice(), nav) { DisplayName = name }
            .PlayAsGuestCommand.ExecuteAsync(null);
        return (api, nav);
    }

    private static async Task PlayCurrentGameAsync(ApiClient api, GameFlow flow, FakeNav nav)
    {
        var quiz = new QuizViewModel(api, flow, nav);
        quiz.Start();
        for (var i = 0; i < flow.CurrentGame!.Questions.Count; i++)
        {
            quiz.SelectChoiceCommand.Execute(quiz.Choices[0]);
            await quiz.ConfirmCommand.ExecuteAsync(null);
        }
        quiz.Dispose();
    }

    [Fact]
    public async Task Duelo_CrearJugarYCompartirCodigo()
    {
        var (api1, nav1) = await LoggedGuestAsync("Reta");
        var flow1 = new GameFlow();
        var share = new NoopShareService();
        var duels1 = new DuelsViewModel(api1, flow1, nav1, share);

        await duels1.CreateCommand.ExecuteAsync(null);
        Assert.Null(duels1.Error);
        Assert.NotNull(duels1.CreatedCode);
        Assert.Contains("quiz", nav1.Routes);
        await PlayCurrentGameAsync(api1, flow1, nav1);

        // Comparte el código.
        await duels1.LoadCommand.ExecuteAsync(null);
        var waiting = duels1.Duels.First();
        await duels1.ShareCodeCommand.ExecuteAsync(waiting);
        Assert.Contains(waiting.Code, share.LastShared);

        // El rival se une por código y juega.
        var (api2, nav2) = await LoggedGuestAsync("Rival");
        var flow2 = new GameFlow();
        var duels2 = new DuelsViewModel(api2, flow2, nav2, new NoopShareService()) { JoinCode = waiting.Code };
        await duels2.JoinCommand.ExecuteAsync(null);
        Assert.Null(duels2.Error);
        await PlayCurrentGameAsync(api2, flow2, nav2);

        await duels2.LoadCommand.ExecuteAsync(null);
        var done = duels2.Duels.First();
        Assert.Equal("Complete", done.Status);
        Assert.NotNull(done.MyScore);
        Assert.NotNull(done.TheirScore);
    }

    [Fact]
    public async Task Misiones_SeVenEnHome_YSeReclamanAlCompletarse()
    {
        var (api, nav) = await LoggedGuestAsync();
        var flow = new GameFlow();
        var home = new HomeViewModel(api, flow, nav);
        await home.LoadCommand.ExecuteAsync(null);
        Assert.Equal(3, home.Quests.Count);

        // Grind universal: completa las 6 condiciones posibles.
        for (var i = 0; i < 3; i++)
        {
            await home.PlayQuickCommand.ExecuteAsync(null);
            await PlayCurrentGameAsync(api, flow, nav); // primera opción: no garantiza perfectas…
        }
        // …así que asegura lo que no depende del azar:
        await home.PlayDailyCommand.ExecuteAsync(null);
        await PlayCurrentGameAsync(api, flow, nav);
        await api.SubmitMinigameAsync("math_sprint", 30, 60_000); // 60 XP y cuenta minijuego
        await api.SubmitMinigameAsync("word_search", 10, 60_000);
        await api.CompleteFocusAsync("calm", 300);

        await home.LoadCommand.ExecuteAsync(null);
        foreach (var q in home.Quests.Where(q => q.Completed && !q.Claimed).ToList())
        {
            await home.ClaimQuestCommand.ExecuteAsync(q);
            Assert.Null(home.Error);
        }
        Assert.Contains(home.Quests, q => q.Claimed);
        Assert.NotNull(home.QuestMessage);
    }

    [Fact]
    public async Task Calibracion_DesdeHome_YRadarEnPerfil()
    {
        var (api, nav) = await LoggedGuestAsync();
        var flow = new GameFlow();
        var home = new HomeViewModel(api, flow, nav);
        await home.LoadCommand.ExecuteAsync(null);
        Assert.True(home.NeedsCalibration);

        await home.PlayCalibrationCommand.ExecuteAsync(null);
        Assert.Equal(10, flow.CurrentGame!.Questions.Count);
        await PlayCurrentGameAsync(api, flow, nav);

        var profile = new ProfileViewModel(api, nav);
        await profile.LoadCommand.ExecuteAsync(null);
        Assert.True(profile.Profile!.Calibrated);
        Assert.Equal(6, profile.Skills.Count);
        Assert.True(profile.Skills.Sum(s => s.Answered) >= 10);
        Assert.Equal(16, profile.AvatarShop.Count);

        await home.LoadCommand.ExecuteAsync(null);
        Assert.False(home.NeedsCalibration);
    }

    [Fact]
    public async Task Avatares_CompraPremiumDesdeElPerfil()
    {
        var (api, nav) = await LoggedGuestAsync();
        // Monedas vía compra sandbox.
        var store = new StoreViewModel(api, new FakePurchaser(), new FakeAds());
        await store.LoadCommand.ExecuteAsync(null);
        await store.BuyCommand.ExecuteAsync(store.Products.First(p => p.ProductId == "braintrain.coins.pack300"));

        var profile = new ProfileViewModel(api, nav);
        await profile.LoadCommand.ExecuteAsync(null);
        var unicorn = profile.AvatarShop.First(a => a.Code == "unicorn");
        await profile.BuyAvatarCommand.ExecuteAsync(unicorn);

        Assert.Null(profile.Error);
        Assert.Equal("unicorn", profile.Profile!.AvatarCode);
        Assert.True(profile.AvatarShop.First(a => a.Code == "unicorn").Owned);
    }

    [Fact]
    public async Task Compartir_ResultadoEstiloWordle()
    {
        var (api, nav) = await LoggedGuestAsync();
        var flow = new GameFlow();
        var home = new HomeViewModel(api, flow, nav);
        await home.PlayDailyCommand.ExecuteAsync(null);
        await PlayCurrentGameAsync(api, flow, nav);

        var share = new NoopShareService();
        var results = new ResultsViewModel(flow, nav, share);
        results.Load();
        await results.ShareCommand.ExecuteAsync(null);

        Assert.NotNull(share.LastShared);
        Assert.Contains("BrainTrain", share.LastShared);
        Assert.Contains("reto diario", share.LastShared);
        Assert.True(share.LastShared!.Contains("🟩") || share.LastShared.Contains("🟥"));
    }

    [Fact]
    public async Task Liga_apareceEnElPerfil()
    {
        var (api, _) = await LoggedGuestAsync();
        var profile = await api.GetProfileAsync();
        Assert.Equal(0, profile.LeagueTier);
        Assert.Equal("Bronce", profile.LeagueName);
    }
}
