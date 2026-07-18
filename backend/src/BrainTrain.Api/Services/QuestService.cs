using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

public sealed record QuestDef(string Code, string Name, string Emoji, int Target, int CoinReward, int XpReward);

/// <summary>
/// Misiones diarias: 3 por día elegidas determinísticamente por usuario+fecha.
/// El progreso sale de contadores diarios en la fila del usuario (reinicio
/// perezoso): cero tablas nuevas, cero jobs.
/// </summary>
public sealed class QuestService(AppDbContext db, AchievementService achievements, IOptions<GameOptions> options)
{
    public static readonly IReadOnlyList<QuestDef> Catalog =
    [
        new("q_sessions3", "Juega 3 partidas", "🎮", 3, 15, 20),
        new("q_perfect1", "Logra una partida perfecta", "🌟", 1, 20, 25),
        new("q_correct20", "Acierta 20 preguntas", "🎯", 20, 15, 20),
        new("q_minigames2", "Entrena con 2 minijuegos", "🧠", 2, 15, 20),
        new("q_daily1", "Completa el reto del día", "⚡", 1, 20, 25),
        new("q_focus1", "Haz una sesión de enfoque", "🧘", 1, 15, 20),
    ];

    /// <summary>Las 3 misiones de hoy para un usuario (mismo set todo el día).</summary>
    public static IReadOnlyList<QuestDef> TodayQuests(long userId, DateOnly todayUtc)
    {
        var seed = HashCode.Combine(userId, todayUtc.DayNumber, 4801);
        var rng = new Random(seed);
        return Catalog.OrderBy(_ => rng.Next()).Take(3).ToList();
    }

    private static int ProgressFor(QuestDef def, User user, bool dailyDone, DateOnly todayUtc) => def.Code switch
    {
        "q_sessions3" => user.SessionsToday,
        "q_perfect1" => user.PerfectsToday,
        "q_correct20" => user.CorrectToday,
        "q_minigames2" => user.MinigamesToday,
        "q_daily1" => dailyDone ? 1 : 0,
        "q_focus1" => user.FocusDateUtc == todayUtc ? 1 : 0, // se marca al completar cualquier sesión de enfoque
        _ => 0
    };

    public async Task<List<QuestDto>> GetTodayAsync(long userId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        ProgressionLogic.EnsureQuestDay(user, today);
        await db.SaveChangesAsync(ct); // persiste el reinicio del día si aplicó

        var dailyDone = await db.DailyChallengeEntries.AsNoTracking()
            .AnyAsync(d => d.UserId == userId && d.DateUtc == today, ct);

        return TodayQuests(userId, today).Select((q, i) =>
        {
            var progress = Math.Min(ProgressFor(q, user, dailyDone, today), q.Target);
            return new QuestDto(q.Code, q.Name, q.Emoji, progress, q.Target,
                q.CoinReward, q.XpReward,
                Completed: progress >= q.Target,
                Claimed: (user.QuestClaimedMask & (1 << i)) != 0);
        }).ToList();
    }

    public async Task<QuestClaimResponse> ClaimAsync(long userId, string code, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        ProgressionLogic.EnsureQuestDay(user, today);

        var todays = TodayQuests(userId, today);
        var index = todays.ToList().FindIndex(q => q.Code == code);
        if (index < 0)
            throw new GameError(404, "quest_not_today", "Esa misión no es de hoy.");
        if ((user.QuestClaimedMask & (1 << index)) != 0)
            throw new GameError(409, "already_claimed", "Ya reclamaste esta misión.");

        var def = todays[index];
        var dailyDone = def.Code == "q_daily1" && await db.DailyChallengeEntries.AsNoTracking()
            .AnyAsync(d => d.UserId == userId && d.DateUtc == today, ct);
        if (ProgressFor(def, user, dailyDone, today) < def.Target)
            throw new GameError(409, "quest_incomplete", "Aún no completas esta misión.");

        ProgressionLogic.EnsureCurrentWeek(user, today);
        user.QuestClaimedMask |= 1 << index;
        user.Coins += def.CoinReward;
        user.CoinsEarnedTotal += def.CoinReward;
        user.Xp += def.XpReward;
        user.WeeklyXp += def.XpReward;
        user.Level = ProgressionLogic.LevelForXp(user.Xp);

        await achievements.EvaluateAsync(user, ct);
        await db.SaveChangesAsync(ct);

        return new QuestClaimResponse(def.CoinReward, def.XpReward,
            ProfileMapper.ToDto(user, options.Value, now));
    }
}
