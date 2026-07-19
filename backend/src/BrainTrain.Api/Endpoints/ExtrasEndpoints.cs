using BrainTrain.Api.Services;
using BrainTrain.Infrastructure;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Endpoints;

/// <summary>Anuncios recompensados, minijuegos de entrenamiento y checkout PayPal del portal.</summary>
public static class ExtrasEndpoints
{
    public static void MapExtras(this IEndpointRouteBuilder app)
    {
        // ---------- Anuncio recompensado: ver un anuncio → +1 vida (tope diario) ----------
        app.MapPost("/api/v1/ads/reward-life", async (HttpContext http, AppDbContext db,
            IOptions<GameOptions> options, CancellationToken ct) =>
        {
            var opt = options.Value;
            var user = await db.Users.FindAsync([http.User.UserId()], ct);
            if (user is null) return Results.Problem(statusCode: 401, title: "user_not_found");

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            if (user.AdRewardDateUtc != today)
            {
                user.AdRewardDateUtc = today;
                user.AdRewardsToday = 0;
            }
            if (user.AdRewardsToday >= opt.AdRewardLivesPerDay)
                return Results.Problem(statusCode: 429, title: "ad_reward_cap",
                    detail: "Ya canjeaste todas las vidas por anuncios de hoy. ¡Vuelve mañana!");

            var (maxLives, regen) = ProgressionLogic.LivesParams(user, opt, now);
            var (lives, anchor, _) = ProgressionLogic.ComputeLives(
                user.Lives, user.LivesUpdatedAtUtc, now, maxLives, regen);
            if (lives >= maxLives)
                return Results.Problem(statusCode: 409, title: "lives_full", detail: "Ya tienes las vidas completas.");

            user.Lives = lives + 1;
            user.LivesUpdatedAtUtc = user.Lives >= maxLives ? now : anchor;
            user.AdRewardsToday++;
            await db.SaveChangesAsync(ct);

            var (livesNow, _, secs) = ProgressionLogic.ComputeLives(
                user.Lives, user.LivesUpdatedAtUtc, now, maxLives, regen);
            return Results.Ok(new AdRewardResponse(
                new LivesDto(livesNow, maxLives, secs),
                opt.AdRewardLivesPerDay - user.AdRewardsToday,
                ProfileMapper.ToDto(user, opt, now)));
        }).RequireAuthorization();

        // ---------- Minijuegos ----------
        app.MapGet("/api/v1/minigames", () => Results.Ok(
                MinigameService.Catalog.Select(g =>
                    new MinigameDto(g.Code, g.Name, g.Emoji, g.Description, g.MaxXpPerSession)).ToList()))
            .CacheOutput("content");

        app.MapPost("/api/v1/minigames/submit", async (MinigameSubmitRequest req, HttpContext http,
                MinigameService minigames, CancellationToken ct) =>
            Results.Ok(await minigames.SubmitAsync(http.User.UserId(), req, ct)))
            .RequireAuthorization();

        // ---------- Práctica offline ----------
        // Devuelve preguntas CON respuesta y explicación para practicar sin
        // conexión. No otorga XP (por eso exponer la respuesta no rompe nada)
        // y de regalo permite feedback instantáneo por pregunta.
        app.MapGet("/api/v1/practice/pack", async (QuestionCatalog catalog, int? count, CancellationToken ct) =>
        {
            var snap = await catalog.GetAsync(ct);
            var n = Math.Clamp(count ?? 40, 10, 80);
            var questions = snap.AllQuestionIds
                .OrderBy(_ => Random.Shared.Next())
                .Take(n)
                .Select(id => snap.QuestionsById[id])
                .Select(q => new PracticeQuestionDto(
                    q.Id, q.CategoryId, q.Type, q.Difficulty, q.Text, q.ImagePath,
                    q.Choices.Select(c => new ChoiceDto(c.Id, c.Text)).ToList(),
                    q.CorrectChoiceId, q.Explanation, q.FunFact))
                .ToList();
            return Results.Ok(new PracticePackDto(DateTime.UtcNow, questions));
        }).RequireAuthorization();

        // ---------- Sesiones de enfoque (flow/respiración/NSDR) ----------
        // XP simbólico y capado: el incentivo es el hábito, no farmear el leaderboard.
        app.MapPost("/api/v1/focus/complete", async (FocusCompleteRequest req, HttpContext http,
            AppDbContext db, IOptions<GameOptions> options, AchievementService achievements, CancellationToken ct) =>
        {
            var opt = options.Value;
            if (req.Seconds < opt.FocusMinSeconds)
                return Results.Problem(statusCode: 422, title: "too_short",
                    detail: $"Una sesión cuenta a partir de {opt.FocusMinSeconds / 60} minutos.");
            if (req.Seconds > 4 * 3600)
                return Results.Problem(statusCode: 422, title: "too_long", detail: "Sesión inválida.");

            var user = await db.Users.FindAsync([http.User.UserId()], ct);
            if (user is null) return Results.Problem(statusCode: 401, title: "user_not_found");

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            if (user.FocusDateUtc != today)
            {
                user.FocusDateUtc = today;
                user.FocusXpToday = 0;
            }

            var xp = Math.Min(opt.FocusXpPerSession, Math.Max(0, opt.FocusDailyXpCap - user.FocusXpToday));
            ProgressionLogic.EnsureCurrentWeek(user, today);
            ProgressionLogic.UpdateStreak(user, today); // cuidar la mente también mantiene la racha
            user.FocusXpToday += xp;
            user.Xp += xp;
            user.WeeklyXp += xp;
            user.Level = ProgressionLogic.LevelForXp(user.Xp);
            await achievements.EvaluateAsync(user, ct);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new FocusResultDto(
                xp, Math.Max(0, opt.FocusDailyXpCap - user.FocusXpToday),
                new StreakDto(user.StreakDays, user.BestStreakDays, user.LastActivityDateUtc == today),
                ProfileMapper.ToDto(user, opt, now)));
        }).RequireAuthorization();

        // ---------- PayPal (portal web) ----------
        app.MapGet("/api/v1/paypal/config", (PayPalCheckoutService paypal) => Results.Ok(paypal.Config()));

        app.MapPost("/api/v1/paypal/create-order", async (PayPalCreateOrderRequest req, HttpContext http,
                PayPalCheckoutService paypal, CancellationToken ct) =>
            Results.Ok(await paypal.CreateOrderAsync(http.User.UserId(), req.ProductId, ct)))
            .RequireAuthorization();

        app.MapPost("/api/v1/paypal/capture", async (PayPalCaptureRequest req, HttpContext http,
                PayPalCheckoutService paypal, CancellationToken ct) =>
            Results.Ok(await paypal.CaptureAsync(http.User.UserId(), req.OrderId, ct)))
            .RequireAuthorization();
    }
}
