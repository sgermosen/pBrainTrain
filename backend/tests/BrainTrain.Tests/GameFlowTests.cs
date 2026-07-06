using System.Net;
using BrainTrain.Api;
using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BrainTrain.Tests;

public class GameFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public GameFlowTests(ApiFactory factory) => _factory = factory;

    private async Task<Dictionary<int, int>> CorrectChoicesAsync(IEnumerable<int> questionIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ids = questionIds.ToArray();
        return await db.Choices.AsNoTracking()
            .Where(c => ids.Contains(c.QuestionId) && c.IsCorrect)
            .ToDictionaryAsync(c => c.QuestionId, c => c.Id);
    }

    [Fact]
    public async Task Categorias_EstanSembradasYPublicas()
    {
        var client = _factory.CreateClient();
        var cats = await (await client.GetAsync("/api/v1/categories")).ReadAs<List<CategoryDto>>();
        Assert.Equal(6, cats.Count);
        Assert.Contains(cats, c => c.Slug == "capciosas");
    }

    [Fact]
    public async Task PartidaCompleta_TodoCorrecto_OtorgaXpLogrosYRacha()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var start = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Quick, null))).ReadAs<StartGameResponse>();

        Assert.Equal(7, start.Questions.Count);
        Assert.Equal(4, start.Lives.Current); // costó 1 vida
        // Seguridad: el payload de preguntas jamás debe filtrar la respuesta correcta.
        Assert.All(start.Questions, q => Assert.True(q.Choices.Count is 2 or 4));

        var correct = await CorrectChoicesAsync(start.Questions.Select(q => q.Id));
        var answers = start.Questions.Select(q => new SubmitAnswer(q.Id, correct[q.Id])).ToList();

        var result = await (await client.PostJson($"/api/v1/game/{start.SessionId}/submit",
            new SubmitGameRequest(answers))).ReadAs<GameResultDto>();

        Assert.Equal(7, result.Correct);
        Assert.True(result.IsPerfect);
        Assert.True(result.XpEarned > 0);
        Assert.Equal(1, result.Streak.Current);
        // Primer logro ("primera partida") debe caer aquí.
        Assert.NotEmpty(result.UnlockedAchievements);
        Assert.True(result.Profile.Xp >= result.XpEarned);
        Assert.All(result.Results, r => Assert.False(string.IsNullOrEmpty(r.Explanation)));

        // Reenviar la misma partida → 409 (anti-replay).
        var again = await client.PostJson($"/api/v1/game/{start.SessionId}/submit", new SubmitGameRequest(answers));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task PartidaPorCategoria_SoloSirvePreguntasDeEsa()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();
        var cats = await (await client.GetAsync("/api/v1/categories")).ReadAs<List<CategoryDto>>();
        var logica = cats.First(c => c.Slug == "logica");

        var start = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Category, logica.Id))).ReadAs<StartGameResponse>();
        Assert.All(start.Questions, q => Assert.Equal(logica.Id, q.CategoryId));
    }

    [Fact]
    public async Task RespuestasAjenas_NoCuentan()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var start = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Quick, null))).ReadAs<StartGameResponse>();

        // Envía respuestas a preguntas que no fueron servidas + una sin respuesta.
        var answers = new List<SubmitAnswer> { new(999999, 1), new(start.Questions[0].Id, null) };
        var result = await (await client.PostJson($"/api/v1/game/{start.SessionId}/submit",
            new SubmitGameRequest(answers))).ReadAs<GameResultDto>();

        Assert.Equal(0, result.Correct);
        Assert.Equal(7, result.Total); // las servidas cuentan como no respondidas
        Assert.False(result.IsPerfect);
    }

    [Fact]
    public async Task SinVidas_NoSePuedeJugar_YLaTiendaLoResuelve()
    {
        // Factoría propia con 2 vidas máximo para agotar rápido.
        using var factory = new SmallLivesApiFactory();
        var client = factory.CreateClient();
        await client.NewGuestAsync();

        for (var i = 0; i < 2; i++)
        {
            var ok = await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Quick, null));
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var sinVidas = await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Quick, null));
        Assert.Equal(HttpStatusCode.Conflict, sinVidas.StatusCode);

        // Compra de prueba (sandbox): recarga vidas y permite volver a jugar.
        var buy = await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "braintrain.lives.refill", $"tx-{Guid.NewGuid():N}", "TEST-OK"));
        var purchase = await buy.ReadAs<PurchaseResponse>();
        Assert.Equal(5, purchase.LivesGranted);

        var otraVez = await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Quick, null));
        Assert.Equal(HttpStatusCode.OK, otraVez.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_MuestraAlJugadorTrasSumarXp()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync("Campeona");

        var start = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Quick, null))).ReadAs<StartGameResponse>();
        var correct = await CorrectChoicesAsync(start.Questions.Select(q => q.Id));
        await client.PostJson($"/api/v1/game/{start.SessionId}/submit",
            new SubmitGameRequest(start.Questions.Select(q => new SubmitAnswer(q.Id, correct[q.Id])).ToList()));

        var board = await (await client.GetAsync("/api/v1/leaderboard/weekly")).ReadAs<LeaderboardDto>();
        Assert.NotNull(board.MyRank);
        Assert.True(board.MyWeeklyXp > 0);
        Assert.Contains(board.Top, e => e.DisplayName == "Campeona");
    }
}
