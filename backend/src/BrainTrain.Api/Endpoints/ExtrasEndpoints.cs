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
