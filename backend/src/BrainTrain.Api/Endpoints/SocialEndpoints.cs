using BrainTrain.Api.Services;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Endpoints;

/// <summary>Duelos 1v1, misiones diarias, tienda de avatares y radar de habilidades.</summary>
public static class SocialEndpoints
{
    public static void MapSocial(this IEndpointRouteBuilder app)
    {
        // ---------------- Duelos ----------------
        var duels = app.MapGroup("/api/v1/duels").RequireAuthorization();

        duels.MapPost("/", async (DuelCreateRequest req, HttpContext http, DuelService svc, CancellationToken ct) =>
            Results.Ok(await svc.CreateAsync(http.User.UserId(), req.OpenToPublic, ct)));

        duels.MapPost("/join", async (DuelJoinRequest req, HttpContext http, DuelService svc, CancellationToken ct) =>
            Results.Ok(await svc.JoinByCodeAsync(http.User.UserId(), req.Code, ct)));

        duels.MapPost("/random", async (HttpContext http, DuelService svc, CancellationToken ct) =>
            Results.Ok(await svc.JoinRandomAsync(http.User.UserId(), ct)));

        duels.MapGet("/mine", async (HttpContext http, DuelService svc, CancellationToken ct) =>
            Results.Ok(await svc.MineAsync(http.User.UserId(), ct)));

        // ---------------- Misiones diarias ----------------
        var quests = app.MapGroup("/api/v1/quests").RequireAuthorization();

        quests.MapGet("/", async (HttpContext http, QuestService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetTodayAsync(http.User.UserId(), ct)));

        quests.MapPost("/{code}/claim", async (string code, HttpContext http, QuestService svc, CancellationToken ct) =>
            Results.Ok(await svc.ClaimAsync(http.User.UserId(), code, ct)));

        // ---------------- Tienda de avatares ----------------
        app.MapGet("/api/v1/avatars", async (HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == http.User.UserId(), ct);
            if (user is null) return Results.Problem(statusCode: 401, title: "user_not_found");
            return Results.Ok(AvatarCatalog.All
                .Select(a => new AvatarShopItemDto(a.Code, a.PriceCoins, AvatarCatalog.Owns(user, a.Code)))
                .ToList());
        }).RequireAuthorization();

        app.MapPost("/api/v1/avatars/buy", async (AvatarBuyRequest req, HttpContext http, AppDbContext db,
            IOptions<GameOptions> opt, CancellationToken ct) =>
        {
            var def = AvatarCatalog.Find(req.Code);
            if (def is null || def.PriceCoins == 0)
                return Results.Problem(statusCode: 400, title: "bad_avatar", detail: "Ese avatar no está a la venta.");

            var user = await db.Users.FindAsync([http.User.UserId()], ct);
            if (user is null) return Results.Problem(statusCode: 401, title: "user_not_found");
            if (AvatarCatalog.Owns(user, req.Code))
                return Results.Problem(statusCode: 409, title: "already_owned", detail: "Ya tienes ese avatar.");
            if (user.Coins < def.PriceCoins)
                return Results.Problem(statusCode: 402, title: "not_enough_coins",
                    detail: $"Necesitas {def.PriceCoins} monedas.");

            user.Coins -= def.PriceCoins;
            user.OwnedAvatarsCsv = user.OwnedAvatarsCsv.Length == 0
                ? req.Code
                : $"{user.OwnedAvatarsCsv},{req.Code}";
            user.AvatarCode = req.Code; // se lo pone de una vez: gratificación inmediata
            await db.SaveChangesAsync(ct);
            return Results.Ok(ProfileMapper.ToDto(user, opt.Value, DateTime.UtcNow));
        }).RequireAuthorization();

        // ---------------- Radar de habilidades ----------------
        app.MapGet("/api/v1/me/skills", async (HttpContext http, AppDbContext db, QuestionCatalog catalog,
            CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            var user = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.CalibratedAtUtc })
                .FirstOrDefaultAsync(ct);
            if (user is null) return Results.Problem(statusCode: 401, title: "user_not_found");

            var stats = await db.UserCategoryStats.AsNoTracking()
                .Where(s => s.UserId == userId)
                .ToDictionaryAsync(s => s.CategoryId, ct);

            var snap = await catalog.GetAsync(ct);
            var skills = snap.Categories.Select(c =>
            {
                var s = stats.GetValueOrDefault(c.Id);
                var answered = s?.Answered ?? 0;
                var correct = s?.Correct ?? 0;
                return new SkillDto(c.Id, c.Slug, c.Name, c.Emoji, answered, correct,
                    answered == 0 ? 0 : Math.Round((double)correct / answered, 3));
            }).ToList();

            return Results.Ok(new SkillsDto(user.CalibratedAtUtc is not null, skills));
        }).RequireAuthorization();
    }
}
