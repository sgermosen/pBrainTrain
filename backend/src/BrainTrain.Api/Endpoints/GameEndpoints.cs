using BrainTrain.Api.Services;
using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BrainTrain.Api.Endpoints;

public static class GameEndpoints
{
    public static void MapGame(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/game").RequireAuthorization();

        g.MapPost("/start", async (StartGameRequest req, HttpContext http, GameService game, CancellationToken ct) =>
            Results.Ok(await game.StartAsync(http.User.UserId(), req, ct)));

        g.MapPost("/{sessionId:guid}/submit", async (Guid sessionId, SubmitGameRequest req, HttpContext http,
                GameService game, CancellationToken ct) =>
            Results.Ok(await game.SubmitAsync(http.User.UserId(), sessionId, req, ct)));

        // Estado del reto diario: si ya se jugó hoy devuelve el resultado; si no, nada más el estado.
        app.MapGet("/api/v1/daily", async (HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var userId = http.User.UserId();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var entry = await db.DailyChallengeEntries.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DateUtc == today, ct);

            return Results.Ok(entry is null
                ? new DailyStatusDto(today, false, null, null, null, null, null)
                : new DailyStatusDto(today, true, entry.CorrectCount, entry.TotalCount, entry.XpEarned, null, null));
        }).RequireAuthorization();
    }
}
