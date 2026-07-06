using BrainTrain.Domain;

namespace BrainTrain.Api.Services;

public static class ProfileMapper
{
    public static ProfileDto ToDto(User user, GameOptions opt, DateTime nowUtc)
    {
        var (lives, _, secs) = ProgressionLogic.ComputeLives(
            user.Lives, user.LivesUpdatedAtUtc, nowUtc, opt.MaxLives, opt.LifeRegenMinutes);

        var levelFloor = ProgressionLogic.XpForLevel(user.Level);
        var nextLevel = ProgressionLogic.XpForLevel(user.Level + 1);
        var today = DateOnly.FromDateTime(nowUtc);

        return new ProfileDto(
            user.Id, user.DisplayName, user.AvatarCode, user.IsGuest, user.Email,
            user.Xp, user.Level,
            XpIntoLevel: user.Xp - levelFloor,
            XpForNextLevel: nextLevel - levelFloor,
            user.Coins,
            new LivesDto(lives, opt.MaxLives, secs),
            new StreakDto(user.StreakDays, user.BestStreakDays, user.LastActivityDateUtc == today),
            user.WeeklyXp,
            new TotalsDto(user.TotalAnswered, user.TotalCorrect, user.SessionsCompleted,
                user.PerfectSessions, user.DailyChallengesCompleted));
    }
}
