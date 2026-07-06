using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

public sealed class AchievementService(AppDbContext db, IMemoryCache cache, IOptions<GameOptions> options)
{
    private const string Key = "achievement-catalog";

    public async Task<IReadOnlyList<Achievement>> GetCatalogAsync(CancellationToken ct)
    {
        if (cache.TryGetValue<IReadOnlyList<Achievement>>(Key, out var list) && list is not null)
            return list;

        list = await db.Achievements.AsNoTracking().OrderBy(a => a.Tier).ThenBy(a => a.Threshold).ToListAsync(ct);
        cache.Set(Key, list, TimeSpan.FromMinutes(options.Value.CatalogCacheMinutes));
        return list;
    }

    /// <summary>
    /// Evalúa y desbloquea logros pendientes según los contadores agregados del
    /// usuario (ya actualizados por el llamador). No hace SaveChanges: se
    /// persiste junto con la partida en una única transacción.
    /// Itera porque las recompensas de XP pueden subir de nivel y desbloquear
    /// a su vez logros de nivel.
    /// </summary>
    public async Task<IReadOnlyList<UnlockedAchievementDto>> EvaluateAsync(User user, CancellationToken ct)
    {
        var all = await GetCatalogAsync(ct);
        var unlockedIds = (await db.UserAchievements.AsNoTracking()
                .Where(ua => ua.UserId == user.Id)
                .Select(ua => ua.AchievementId)
                .ToListAsync(ct))
            .ToHashSet();

        Dictionary<int, int>? categoryCorrect = null;
        if (all.Any(a => a.CriteriaType == AchievementCriteria.CategoryCorrect && !unlockedIds.Contains(a.Id)))
        {
            categoryCorrect = await db.UserCategoryStats.AsNoTracking()
                .Where(s => s.UserId == user.Id)
                .ToDictionaryAsync(s => s.CategoryId, s => s.Correct, ct);
        }

        var newlyUnlocked = new List<UnlockedAchievementDto>();
        var now = DateTime.UtcNow;

        for (var pass = 0; pass < 3; pass++)
        {
            var any = false;
            foreach (var a in all)
            {
                if (unlockedIds.Contains(a.Id) || !IsMet(a, user, categoryCorrect))
                    continue;

                unlockedIds.Add(a.Id);
                db.UserAchievements.Add(new UserAchievement
                {
                    UserId = user.Id,
                    AchievementId = a.Id,
                    UnlockedAtUtc = now
                });
                user.Xp += a.XpReward;
                user.WeeklyXp += a.XpReward;
                user.Coins += a.CoinReward;
                user.CoinsEarnedTotal += a.CoinReward;
                user.Level = ProgressionLogic.LevelForXp(user.Xp);
                newlyUnlocked.Add(new UnlockedAchievementDto(
                    a.Code, a.Name, a.Description, a.Emoji, a.Tier, a.XpReward, a.CoinReward));
                any = true;
            }
            if (!any) break;
        }

        return newlyUnlocked;
    }

    private static bool IsMet(Achievement a, User u, Dictionary<int, int>? categoryCorrect) => a.CriteriaType switch
    {
        AchievementCriteria.SessionsCompleted => u.SessionsCompleted >= a.Threshold,
        AchievementCriteria.CorrectAnswers => u.TotalCorrect >= a.Threshold,
        AchievementCriteria.PerfectSessions => u.PerfectSessions >= a.Threshold,
        AchievementCriteria.StreakDays => u.BestStreakDays >= a.Threshold,
        AchievementCriteria.DailyChallengesCompleted => u.DailyChallengesCompleted >= a.Threshold,
        AchievementCriteria.LevelReached => u.Level >= a.Threshold,
        AchievementCriteria.CoinsEarned => u.CoinsEarnedTotal >= a.Threshold,
        AchievementCriteria.CategoryCorrect =>
            a.CategoryId is not null
            && categoryCorrect is not null
            && categoryCorrect.GetValueOrDefault(a.CategoryId.Value) >= a.Threshold,
        _ => false
    };

    /// <summary>Progreso actual del usuario hacia un logro (para la pantalla de logros).</summary>
    public static int ProgressFor(Achievement a, User u, Dictionary<int, int> categoryCorrect) => a.CriteriaType switch
    {
        AchievementCriteria.SessionsCompleted => u.SessionsCompleted,
        AchievementCriteria.CorrectAnswers => u.TotalCorrect,
        AchievementCriteria.PerfectSessions => u.PerfectSessions,
        AchievementCriteria.StreakDays => u.BestStreakDays,
        AchievementCriteria.DailyChallengesCompleted => u.DailyChallengesCompleted,
        AchievementCriteria.LevelReached => u.Level,
        AchievementCriteria.CoinsEarned => u.CoinsEarnedTotal,
        AchievementCriteria.CategoryCorrect =>
            a.CategoryId is null ? 0 : categoryCorrect.GetValueOrDefault(a.CategoryId.Value),
        _ => 0
    };
}
