using System.Net;
using BrainTrain.Api;
using BrainTrain.Domain;

namespace BrainTrain.Tests;

public class DailyAndStoreTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public DailyAndStoreTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task RetoDiario_NoCuestaVida_DaBonus_YSoloUnaVezPorDia()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var estado = await (await client.GetAsync("/api/v1/daily")).ReadAs<DailyStatusDto>();
        Assert.False(estado.Completed);

        var start = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Daily, null))).ReadAs<StartGameResponse>();
        Assert.Equal(5, start.Lives.Current); // el reto diario no consume vidas

        // Mismo día: si vuelve a pedir el reto recibe la MISMA sesión (no re-baraja).
        var startAgain = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Daily, null))).ReadAs<StartGameResponse>();
        Assert.Equal(start.SessionId, startAgain.SessionId);

        // Responde todo en blanco: completa igual (participación cuenta para la racha).
        var result = await (await client.PostJson($"/api/v1/game/{start.SessionId}/submit",
            new SubmitGameRequest([]))).ReadAs<GameResultDto>();
        Assert.True(result.XpEarned >= 50); // bonus fijo del reto diario
        Assert.Equal(1, result.Profile.Totals.DailyChallenges);

        var estado2 = await (await client.GetAsync("/api/v1/daily")).ReadAs<DailyStatusDto>();
        Assert.True(estado2.Completed);

        // Un segundo intento el mismo día se rechaza.
        var otra = await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Daily, null));
        Assert.Equal(HttpStatusCode.Conflict, otra.StatusCode);
    }

    [Fact]
    public async Task Tienda_CatalogoPublico_CompraValida_YAntiReplay()
    {
        var client = _factory.CreateClient();

        var catalogo = await (await client.GetAsync("/api/v1/store/catalog")).ReadAs<StoreCatalogDto>();
        Assert.NotEmpty(catalogo.Products);
        Assert.True(catalogo.RefillCoinCost > 0);

        await client.NewGuestAsync();
        var tx = $"tx-{Guid.NewGuid():N}";

        var compra = await (await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "braintrain.coins.pack300", tx, "TEST-OK"))).ReadAs<PurchaseResponse>();
        Assert.Equal(300, compra.CoinsGranted);
        Assert.Equal(300, compra.Profile.Coins);

        // El mismo recibo no se puede canjear dos veces.
        var replay = await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "braintrain.coins.pack300", tx, "TEST-OK"));
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);

        // Recibo inválido → 402.
        var invalido = await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "braintrain.coins.pack300", $"tx-{Guid.NewGuid():N}", "FALSO"));
        Assert.Equal(HttpStatusCode.PaymentRequired, invalido.StatusCode);

        // Producto inexistente → 400.
        var noProducto = await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "no.existe", $"tx-{Guid.NewGuid():N}", "TEST-OK"));
        Assert.Equal(HttpStatusCode.BadRequest, noProducto.StatusCode);
    }

    [Fact]
    public async Task RecargaConMonedas_FuncionaYValidaSaldo()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        // Sin monedas y con vidas llenas → primero 409 (vidas llenas).
        var llenas = await client.PostJson("/api/v1/store/refill-with-coins", new { });
        Assert.Equal(HttpStatusCode.Conflict, llenas.StatusCode);

        // Gasta una vida para poder recargar.
        await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Quick, null));

        // Sin monedas suficientes → 402.
        var sinMonedas = await client.PostJson("/api/v1/store/refill-with-coins", new { });
        Assert.Equal(HttpStatusCode.PaymentRequired, sinMonedas.StatusCode);

        // Compra monedas (sandbox) y recarga.
        await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "braintrain.coins.pack300", $"tx-{Guid.NewGuid():N}", "TEST-OK"));
        var perfil = await (await client.PostJson("/api/v1/store/refill-with-coins", new { })).ReadAs<ProfileDto>();
        Assert.Equal(5, perfil.Lives.Current);
        Assert.Equal(200, perfil.Coins); // 300 - 100 de la recarga
    }

    [Fact]
    public async Task Logros_ListanConProgreso()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var logros = await (await client.GetAsync("/api/v1/me/achievements")).ReadAs<List<AchievementDto>>();
        Assert.True(logros.Count >= 20);
        Assert.All(logros, a => Assert.False(a.Unlocked));

        // Juega una partida y verifica que el logro de primera partida quede desbloqueado.
        var start = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Quick, null))).ReadAs<StartGameResponse>();
        await client.PostJson($"/api/v1/game/{start.SessionId}/submit", new SubmitGameRequest([]));

        logros = await (await client.GetAsync("/api/v1/me/achievements")).ReadAs<List<AchievementDto>>();
        Assert.Contains(logros, a => a.Unlocked);
    }

    [Fact]
    public async Task TokenDeDispositivo_SeRegistra()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();
        var resp = await client.PostJson("/api/v1/me/devices",
            new DeviceTokenRequest(StorePlatform.GooglePlay, $"fcm-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }
}
