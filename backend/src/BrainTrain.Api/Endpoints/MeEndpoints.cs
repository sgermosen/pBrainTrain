using BrainTrain.Api.Services;
using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMe(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/me").RequireAuthorization();

        g.MapGet("/", async (HttpContext http, AppDbContext db, IOptions<GameOptions> opt, CancellationToken ct) =>
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == http.User.UserId(), ct);
            return user is null
                ? Results.Problem(statusCode: 401, title: "user_not_found")
                : Results.Ok(ProfileMapper.ToDto(user, opt.Value, DateTime.UtcNow));
        });

        g.MapPatch("/", async (UpdateProfileRequest req, HttpContext http, AppDbContext db,
            IOptions<GameOptions> opt, CancellationToken ct) =>
        {
            var user = await db.Users.FindAsync([http.User.UserId()], ct);
            if (user is null) return Results.Problem(statusCode: 401, title: "user_not_found");

            if (req.DisplayName is not null)
            {
                var name = req.DisplayName.Trim();
                if (name.Length is < 2 or > 40)
                    return Results.Problem(statusCode: 400, title: "bad_name", detail: "El nombre debe tener entre 2 y 40 caracteres.");
                user.DisplayName = name;
            }
            if (req.AvatarCode is not null)
            {
                if (!AvatarCatalog.Owns(user, req.AvatarCode))
                    return Results.Problem(statusCode: 400, title: "bad_avatar",
                        detail: "Avatar no disponible: cómpralo en la tienda de avatares.");
                user.AvatarCode = req.AvatarCode;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ProfileMapper.ToDto(user, opt.Value, DateTime.UtcNow));
        });

        g.MapGet("/lives", async (HttpContext http, AppDbContext db, IOptions<GameOptions> opt, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            var row = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Lives, u.LivesUpdatedAtUtc, u.PremiumUntilUtc })
                .FirstOrDefaultAsync(ct);
            if (row is null) return Results.Problem(statusCode: 401, title: "user_not_found");

            var now = DateTime.UtcNow;
            var premium = row.PremiumUntilUtc is { } until && until > now;
            var maxLives = premium ? opt.Value.PremiumMaxLives : opt.Value.MaxLives;
            var regen = premium ? opt.Value.PremiumLifeRegenMinutes : opt.Value.LifeRegenMinutes;
            var (lives, _, secs) = ProgressionLogic.ComputeLives(
                row.Lives, row.LivesUpdatedAtUtc, now, maxLives, regen);
            return Results.Ok(new LivesDto(lives, maxLives, secs));
        });

        g.MapGet("/achievements", async (HttpContext http, AppDbContext db, AchievementService achievements,
            CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Results.Problem(statusCode: 401, title: "user_not_found");

            var all = await achievements.GetCatalogAsync(ct);
            var mine = await db.UserAchievements.AsNoTracking()
                .Where(ua => ua.UserId == userId)
                .ToDictionaryAsync(ua => ua.AchievementId, ua => ua.UnlockedAtUtc, ct);
            var categoryCorrect = await db.UserCategoryStats.AsNoTracking()
                .Where(s => s.UserId == userId)
                .ToDictionaryAsync(s => s.CategoryId, s => s.Correct, ct);

            var list = all.Select(a =>
            {
                var unlocked = mine.TryGetValue(a.Id, out var at);
                return new AchievementDto(
                    a.Code, a.Name, a.Description, a.Emoji, a.Tier, a.XpReward, a.CoinReward,
                    a.Threshold, a.CriteriaType,
                    Math.Min(AchievementService.ProgressFor(a, user, categoryCorrect), a.Threshold),
                    unlocked, unlocked ? at : null);
            }).ToList();

            return Results.Ok(list);
        });

        g.MapPost("/devices", async (DeviceTokenRequest req, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Token) || req.Token.Length > 512)
                return Results.Problem(statusCode: 400, title: "bad_token");

            var userId = http.User.UserId();
            var existing = await db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == req.Token, ct);
            if (existing is null)
            {
                db.DeviceTokens.Add(new DeviceToken
                {
                    UserId = userId, Platform = req.Platform, Token = req.Token, UpdatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.UserId = userId;
                existing.Platform = req.Platform;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
