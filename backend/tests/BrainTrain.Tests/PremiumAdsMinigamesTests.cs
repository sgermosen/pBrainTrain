using System.Net;
using BrainTrain.Api;
using BrainTrain.Domain;

namespace BrainTrain.Tests;

public class PremiumAdsMinigamesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PremiumAdsMinigamesTests(ApiFactory factory) => _factory = factory;

    // ------------------------------------------------------------- Premium
    [Fact]
    public async Task Premium_SubeTopeDeVidasYQuitaAnuncios()
    {
        var client = _factory.CreateClient();
        var auth = await client.NewGuestAsync();
        Assert.False(auth.Profile.IsPremium);
        Assert.True(auth.Profile.ShowAds);
        Assert.Equal(5, auth.Profile.Lives.Max);

        var buy = await (await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "braintrain.premium.month", $"tx-{Guid.NewGuid():N}", "TEST-OK")))
            .ReadAs<PurchaseResponse>();

        Assert.True(buy.Profile.IsPremium);
        Assert.False(buy.Profile.ShowAds);
        Assert.Equal(8, buy.Profile.Lives.Max);
        Assert.NotNull(buy.Profile.PremiumUntilUtc);
        Assert.True(buy.Profile.PremiumUntilUtc > DateTime.UtcNow.AddDays(29));

        // Comprar de nuevo extiende desde el vencimiento (no reinicia).
        var buy2 = await (await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "braintrain.premium.month", $"tx-{Guid.NewGuid():N}", "TEST-OK")))
            .ReadAs<PurchaseResponse>();
        Assert.True(buy2.Profile.PremiumUntilUtc > DateTime.UtcNow.AddDays(59));
    }

    // ------------------------------------------------ Anuncios recompensados
    [Fact]
    public async Task AnuncioRecompensado_DaUnaVidaConTopeDiario()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        // Con vidas llenas → 409.
        var full = await client.PostJson("/api/v1/ads/reward-life", new { });
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);

        // Gasta una vida jugando y canjea el anuncio.
        await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Quick, null));
        var reward = await (await client.PostJson("/api/v1/ads/reward-life", new { })).ReadAs<AdRewardResponse>();
        Assert.Equal(5, reward.Lives.Current);
        Assert.Equal(4, reward.RemainingToday);
    }

    [Fact]
    public async Task AnuncioRecompensado_RespetaElTope()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        // Alterna gastar vida / canjear anuncio hasta agotar el tope de 5.
        for (var i = 0; i < 5; i++)
        {
            await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Quick, null));
            var r = await client.PostJson("/api/v1/ads/reward-life", new { });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }
        await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Quick, null));
        var capped = await client.PostJson("/api/v1/ads/reward-life", new { });
        Assert.Equal(HttpStatusCode.TooManyRequests, capped.StatusCode);
    }

    // ------------------------------------------------------------ Minijuegos
    [Fact]
    public async Task Minijuegos_CatalogoPublico()
    {
        var client = _factory.CreateClient();
        var games = await (await client.GetAsync("/api/v1/minigames")).ReadAs<List<MinigameDto>>();
        Assert.Equal(10, games.Count);
        Assert.Contains(games, g => g.Code == "g2048");
        Assert.Contains(games, g => g.Code == "math_sprint");
        Assert.Contains(games, g => g.Code == "word_search");
        Assert.Contains(games, g => g.Code == "memory_pairs");
        Assert.Contains(games, g => g.Code == "simon");
        Assert.Contains(games, g => g.Code == "spot_diff");
        Assert.Contains(games, g => g.Code == "rubik_guide");
    }

    [Fact]
    public async Task Minijuego_OtorgaXpMonedasYRacha()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var result = await (await client.PostJson("/api/v1/minigames/submit",
            new MinigameSubmitRequest("math_sprint", 25, 60_000))).ReadAs<MinigameResultDto>();

        Assert.Equal(50, result.XpEarned); // 25 aciertos × 2 XP
        Assert.Equal(5, result.CoinsEarned);
        Assert.Equal(1, result.Streak.Current); // entrenar cuenta para la racha
        Assert.True(result.Profile.Xp >= 50);
    }

    [Fact]
    public async Task Minijuego_ValidaAbusos()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        // Juego desconocido.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostJson("/api/v1/minigames/submit",
            new MinigameSubmitRequest("no_existe", 10, 60_000))).StatusCode);

        // Demasiado rápido (bot).
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostJson("/api/v1/minigames/submit",
            new MinigameSubmitRequest("math_sprint", 10, 1_000))).StatusCode);

        // Puntaje imposible.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostJson("/api/v1/minigames/submit",
            new MinigameSubmitRequest("math_sprint", 9999, 60_000))).StatusCode);
    }

    [Fact]
    public async Task Minijuego_TopeDiarioDeXp()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        // Cap diario = 300 XP. math_sprint da máx. 60 por sesión → a la 6ª ya no da XP.
        var total = 0;
        for (var i = 0; i < 6; i++)
        {
            var r = await (await client.PostJson("/api/v1/minigames/submit",
                new MinigameSubmitRequest("math_sprint", 30, 60_000))).ReadAs<MinigameResultDto>();
            total += r.XpEarned;
        }
        Assert.Equal(300, total);

        var extra = await (await client.PostJson("/api/v1/minigames/submit",
            new MinigameSubmitRequest("math_sprint", 30, 60_000))).ReadAs<MinigameResultDto>();
        Assert.Equal(0, extra.XpEarned);
        Assert.Equal(0, extra.DailyXpRemaining);
    }

    // ---------------------------------------------------------------- PayPal
    [Fact]
    public async Task PayPal_FlujoCompletoAcreditaElProducto()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var cfg = await (await client.GetAsync("/api/v1/paypal/config")).ReadAs<PayPalConfigDto>();
        Assert.True(cfg.Enabled);

        var order = await (await client.PostJson("/api/v1/paypal/create-order",
            new PayPalCreateOrderRequest("braintrain.coins.pack300"))).ReadAs<PayPalCreateOrderResponse>();
        Assert.StartsWith("PPORDER-", order.OrderId);

        var captured = await (await client.PostJson("/api/v1/paypal/capture",
            new PayPalCaptureRequest(order.OrderId))).ReadAs<PurchaseResponse>();
        Assert.Equal(300, captured.CoinsGranted);
        Assert.Equal(300, captured.Profile.Coins);

        // La misma orden no se captura dos veces (el gateway la consume).
        var again = await client.PostJson("/api/v1/paypal/capture", new PayPalCaptureRequest(order.OrderId));
        Assert.Equal(HttpStatusCode.PaymentRequired, again.StatusCode);
    }

    [Fact]
    public async Task PayPal_PremiumDesdeElPortal()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var order = await (await client.PostJson("/api/v1/paypal/create-order",
            new PayPalCreateOrderRequest("braintrain.premium.month"))).ReadAs<PayPalCreateOrderResponse>();
        var captured = await (await client.PostJson("/api/v1/paypal/capture",
            new PayPalCaptureRequest(order.OrderId))).ReadAs<PurchaseResponse>();
        Assert.True(captured.Profile.IsPremium);
    }

    [Fact]
    public async Task PayPal_NoCapturaOrdenesDeOtraCuenta()
    {
        var client1 = _factory.CreateClient();
        await client1.NewGuestAsync();
        var order = await (await client1.PostJson("/api/v1/paypal/create-order",
            new PayPalCreateOrderRequest("braintrain.coins.pack300"))).ReadAs<PayPalCreateOrderResponse>();

        var client2 = _factory.CreateClient();
        await client2.NewGuestAsync();
        var stolen = await client2.PostJson("/api/v1/paypal/capture", new PayPalCaptureRequest(order.OrderId));
        Assert.Equal(HttpStatusCode.Forbidden, stolen.StatusCode);
    }

    [Fact]
    public async Task Portal_SeSirveEstaticamente()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/portal/index.html");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Portal de recargas", html);
    }
}
