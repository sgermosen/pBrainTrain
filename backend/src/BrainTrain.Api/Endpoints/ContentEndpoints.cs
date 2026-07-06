using BrainTrain.Api.Services;
using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Endpoints;

public static class ContentEndpoints
{
    public static void MapContent(this IEndpointRouteBuilder app)
    {
        // Público y cacheado en el servidor: el contenido es igual para todos.
        app.MapGet("/api/v1/categories", async (QuestionCatalog catalog, CancellationToken ct) =>
                Results.Ok((await catalog.GetAsync(ct)).Categories))
            .CacheOutput("content");

        app.MapGet("/api/v1/leaderboard/weekly", async (HttpContext http, AppDbContext db,
            IOptions<GameOptions> opt, CancellationToken ct) =>
        {
            var week = ProgressionLogic.WeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

            // Top 50 de la semana. La consulta usa el índice (WeekStartUtc, WeeklyXp DESC).
            var top = await db.Users.AsNoTracking()
                .Where(u => u.WeekStartUtc == week && u.WeeklyXp > 0)
                .OrderByDescending(u => u.WeeklyXp).ThenBy(u => u.Id)
                .Take(50)
                .Select(u => new { u.DisplayName, u.AvatarCode, u.WeeklyXp, u.Level })
                .ToListAsync(ct);

            var entries = top.Select((u, i) =>
                new LeaderboardEntryDto(i + 1, u.DisplayName, u.AvatarCode, u.WeeklyXp, u.Level)).ToList();

            int? myRank = null;
            var myWeekly = 0;
            if (http.User.Identity?.IsAuthenticated == true)
            {
                var userId = http.User.UserId();
                var me = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.WeeklyXp, u.WeekStartUtc })
                    .FirstOrDefaultAsync(ct);
                if (me is not null && me.WeekStartUtc == week && me.WeeklyXp > 0)
                {
                    myWeekly = me.WeeklyXp;
                    myRank = 1 + await db.Users.AsNoTracking()
                        .CountAsync(u => u.WeekStartUtc == week && u.WeeklyXp > me.WeeklyXp, ct);
                }
            }

            return Results.Ok(new LeaderboardDto(entries, myRank, myWeekly));
        });
    }
}
