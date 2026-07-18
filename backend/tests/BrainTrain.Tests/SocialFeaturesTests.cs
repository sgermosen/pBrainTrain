using System.Net;
using System.Net.Http.Json;
using BrainTrain.Api;
using BrainTrain.Api.Services;
using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BrainTrain.Tests;

public class SocialFeaturesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SocialFeaturesTests(ApiFactory factory) => _factory = factory;

    private async Task<Dictionary<int, int>> CorrectChoicesAsync(IEnumerable<int> questionIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ids = questionIds.ToArray();
        return await db.Choices.AsNoTracking()
            .Where(c => ids.Contains(c.QuestionId) && c.IsCorrect)
            .ToDictionaryAsync(c => c.QuestionId, c => c.Id);
    }

    private async Task SubmitAsync(HttpClient client, Guid sessionId, IReadOnlyList<QuestionDto> questions, int correctCount)
    {
        var correct = await CorrectChoicesAsync(questions.Select(q => q.Id));
        var answers = questions.Select((q, i) =>
            new SubmitAnswer(q.Id, i < correctCount ? correct[q.Id] : null)).ToList();
        var resp = await client.PostJson($"/api/v1/game/{sessionId}/submit", new SubmitGameRequest(answers));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ---------------------------------------------------------------- Duelos
    [Fact]
    public async Task Duelo_FlujoCompletoConGanadorYBonus()
    {
        var c1 = _factory.CreateClient();
        await c1.NewGuestAsync("Retadora");
        var c2 = _factory.CreateClient();
        await c2.NewGuestAsync("Rival");

        // La retadora crea el duelo y juega perfecto.
        var start = await (await c1.PostJson("/api/v1/duels", new DuelCreateRequest(false))).ReadAs<DuelStartResponse>();
        Assert.Equal(DuelStatus.WaitingOpponent, start.Duel.Status);
        Assert.Equal(6, start.Duel.Code.Length);
        Assert.Equal(7, start.Questions.Count);
        Assert.Equal(4, start.Lives.Current); // costó una vida
        await SubmitAsync(c1, start.SessionId, start.Questions, correctCount: 7);

        // El rival se une por código y falla dos.
        var join = await (await c2.PostJson("/api/v1/duels/join", new DuelJoinRequest(start.Duel.Code))).ReadAs<DuelStartResponse>();
        Assert.Equal(start.Questions.Select(q => q.Id), join.Questions.Select(q => q.Id)); // mismas preguntas
        await SubmitAsync(c2, join.SessionId, join.Questions, correctCount: 5);

        // Ambos ven el duelo completo con los puntajes correctos.
        var mine1 = await (await c1.GetAsync("/api/v1/duels/mine")).ReadAs<List<DuelDto>>();
        var d1 = mine1.First(d => d.Id == start.Duel.Id);
        Assert.Equal(DuelStatus.Complete, d1.Status);
        Assert.Equal(7, d1.MyScore);
        Assert.Equal(5, d1.TheirScore);
        Assert.Equal("Rival", d1.OpponentName);

        var mine2 = await (await c2.GetAsync("/api/v1/duels/mine")).ReadAs<List<DuelDto>>();
        var d2 = mine2.First(d => d.Id == start.Duel.Id);
        Assert.Equal(5, d2.MyScore);
        Assert.Equal(7, d2.TheirScore);
    }

    [Fact]
    public async Task Duelo_NoPuedesJugarContraTiMismo()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();
        var start = await (await client.PostJson("/api/v1/duels", new DuelCreateRequest(false))).ReadAs<DuelStartResponse>();
        var self = await client.PostJson("/api/v1/duels/join", new DuelJoinRequest(start.Duel.Code));
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
    }

    [Fact]
    public async Task Duelo_EmparejamientoAleatorioReutilizaDuelosAbiertos()
    {
        var c1 = _factory.CreateClient();
        await c1.NewGuestAsync();
        var c2 = _factory.CreateClient();
        await c2.NewGuestAsync();

        var a = await (await c1.PostJson("/api/v1/duels/random", new { })).ReadAs<DuelStartResponse>();
        Assert.Equal(DuelStatus.WaitingOpponent, a.Duel.Status);

        var b = await (await c2.PostJson("/api/v1/duels/random", new { })).ReadAs<DuelStartResponse>();
        Assert.Equal(a.Duel.Id, b.Duel.Id); // se unió al abierto en vez de crear otro
        Assert.Equal(DuelStatus.InProgress, b.Duel.Status);
    }

    // ---------------------------------------------------------------- Misiones
    [Fact]
    public async Task Misiones_TresPorDia_SeCompletanYReclaman()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var quests = await (await client.GetAsync("/api/v1/quests")).ReadAs<List<QuestDto>>();
        Assert.Equal(3, quests.Count);
        Assert.All(quests, q => Assert.False(q.Claimed));

        // "Grind universal": completa las 6 condiciones posibles para que las 3
        // misiones de hoy (sean cuales sean) queden completas.
        for (var i = 0; i < 3; i++)
        {
            var s = await (await client.PostJson("/api/v1/game/start",
                new StartGameRequest(GameMode.Quick, null))).ReadAs<StartGameResponse>();
            await SubmitAsync(client, s.SessionId, s.Questions, correctCount: s.Questions.Count); // perfectas
        }
        var daily = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Daily, null))).ReadAs<StartGameResponse>();
        await SubmitAsync(client, daily.SessionId, daily.Questions, correctCount: 0);
        await client.PostJson("/api/v1/minigames/submit", new MinigameSubmitRequest("math_sprint", 5, 60_000));
        await client.PostJson("/api/v1/minigames/submit", new MinigameSubmitRequest("word_search", 3, 60_000));
        await client.PostJson("/api/v1/focus/complete", new FocusCompleteRequest("calm", 300));

        quests = await (await client.GetAsync("/api/v1/quests")).ReadAs<List<QuestDto>>();
        Assert.All(quests, q => Assert.True(q.Completed, $"{q.Code} debería estar completa"));

        foreach (var q in quests)
        {
            var claim = await (await client.PostJson($"/api/v1/quests/{q.Code}/claim", new { })).ReadAs<QuestClaimResponse>();
            Assert.Equal(q.CoinReward, claim.CoinsGranted);
        }

        // Reclamar dos veces → 409.
        var again = await client.PostJson($"/api/v1/quests/{quests[0].Code}/claim", new { });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Misiones_NoSeReclamanIncompletas()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();
        var quests = await (await client.GetAsync("/api/v1/quests")).ReadAs<List<QuestDto>>();
        var claim = await client.PostJson($"/api/v1/quests/{quests[0].Code}/claim", new { });
        Assert.Equal(HttpStatusCode.Conflict, claim.StatusCode);
    }

    // ---------------------------------------------------------------- Avatares
    [Fact]
    public async Task Avatares_CompraYEquipaConMonedas()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var shop = await (await client.GetAsync("/api/v1/avatars")).ReadAs<List<AvatarShopItemDto>>();
        Assert.Equal(16, shop.Count);
        Assert.Equal(10, shop.Count(a => a.PriceCoins == 0));

        // Sin monedas → 402.
        var broke = await client.PostJson("/api/v1/avatars/buy", new AvatarBuyRequest("unicorn"));
        Assert.Equal(HttpStatusCode.PaymentRequired, broke.StatusCode);

        // No se puede equipar un premium sin comprarlo.
        var patch = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequest(null, "unicorn"), options: TestClientExtensions.Json)
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(patch)).StatusCode);

        // Compra monedas (sandbox) → compra el avatar → queda equipado.
        await client.PostJson("/api/v1/store/purchase",
            new PurchaseRequest(StorePlatform.Test, "braintrain.coins.pack300", $"tx-{Guid.NewGuid():N}", "TEST-OK"));
        var profile = await (await client.PostJson("/api/v1/avatars/buy", new AvatarBuyRequest("unicorn"))).ReadAs<ProfileDto>();
        Assert.Equal("unicorn", profile.AvatarCode);
        Assert.Equal(150, 300 - profile.Coins);

        var again = await client.PostJson("/api/v1/avatars/buy", new AvatarBuyRequest("unicorn"));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    // ------------------------------------------------------------ Calibración
    [Fact]
    public async Task Calibracion_10PreguntasSinVidas_YSiembraElRadar()
    {
        var client = _factory.CreateClient();
        var auth = await client.NewGuestAsync();
        Assert.False(auth.Profile.Calibrated);

        var start = await (await client.PostJson("/api/v1/game/start",
            new StartGameRequest(GameMode.Calibration, null))).ReadAs<StartGameResponse>();
        Assert.Equal(10, start.Questions.Count);
        Assert.Equal(5, start.Lives.Current); // no gasta vidas
        Assert.True(start.Questions.Select(q => q.CategoryId).Distinct().Count() >= 4); // variedad

        await SubmitAsync(client, start.SessionId, start.Questions, correctCount: 6);

        var skills = await (await client.GetAsync("/api/v1/me/skills")).ReadAs<SkillsDto>();
        Assert.True(skills.Calibrated);
        Assert.Equal(6, skills.Skills.Count);
        Assert.True(skills.Skills.Sum(s => s.Answered) >= 10);

        // Solo una vez.
        var second = await client.PostJson("/api/v1/game/start", new StartGameRequest(GameMode.Calibration, null));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ---------------------------------------------------------------- Ligas
    [Fact]
    public void Ligas_PromocionYDescensoPorUmbralSemanal()
    {
        var monday = new DateOnly(2026, 7, 6);
        var nextWeek = monday.AddDays(7);

        // Sube: Bronce con 200 XP (umbral 150).
        var u = new User { WeekStartUtc = monday, WeeklyXp = 200, LeagueTier = 0 };
        ProgressionLogic.EnsureCurrentWeek(u, nextWeek);
        Assert.Equal(1, u.LeagueTier);
        Assert.Equal(0, u.WeeklyXp);

        // Baja: Oro (tier 2) con 50 XP (< mínimo 100).
        u = new User { WeekStartUtc = monday, WeeklyXp = 50, LeagueTier = 2 };
        ProgressionLogic.EnsureCurrentWeek(u, nextWeek);
        Assert.Equal(1, u.LeagueTier);

        // Se mantiene: Plata con 120 XP (≥ mínimo 50, < promoción 300).
        u = new User { WeekStartUtc = monday, WeeklyXp = 120, LeagueTier = 1 };
        ProgressionLogic.EnsureCurrentWeek(u, nextWeek);
        Assert.Equal(1, u.LeagueTier);

        // Leyenda no baja con buen XP y no puede subir más.
        u = new User { WeekStartUtc = monday, WeeklyXp = 900, LeagueTier = 4 };
        ProgressionLogic.EnsureCurrentWeek(u, nextWeek);
        Assert.Equal(4, u.LeagueTier);
    }

    // ---------------------------------------------------------------- Admin
    [Fact]
    public async Task Admin_ProtegidoPorClave_YGestionaPreguntas()
    {
        var client = _factory.CreateClient();

        // Sin clave → 401 (la clave sí está configurada en la fixture).
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/stats")).StatusCode);

        client.DefaultRequestHeaders.Add("X-Admin-Key", "test-admin-key-1234567890");
        var stats = await (await client.GetAsync("/api/admin/stats")).ReadAs<AdminStatsDto>();
        Assert.True(stats.QuestionsActive >= 160);

        // Crear una pregunta nueva y verla en el catálogo del juego.
        var created = await (await client.PostJson("/api/admin/questions", new AdminQuestionRequest(
            CategoryId: 1, QuestionType.MultipleChoice, Difficulty: 2,
            Text: $"¿Pregunta admin {Guid.NewGuid():N}?",
            Explanation: "Porque sí, con lógica.", FunFact: null,
            Choices:
            [
                new AdminChoiceDto("Correcta", true),
                new AdminChoiceDto("Mala 1", false),
                new AdminChoiceDto("Mala 2", false),
                new AdminChoiceDto("Mala 3", false)
            ]))).ReadAs<AdminQuestionDto>();
        Assert.True(created.Id > 0);

        // Validación: dos correctas → 400.
        var bad = await client.PostJson("/api/admin/questions", new AdminQuestionRequest(
            1, QuestionType.TrueFalse, 1, "¿Mal?", "expl", null,
            [new AdminChoiceDto("V", true), new AdminChoiceDto("F", true)]));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        // Borrado suave.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/questions/{created.Id}")).StatusCode);
    }
}
