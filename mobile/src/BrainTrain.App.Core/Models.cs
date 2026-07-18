namespace BrainTrain.App.Core;

// Modelos espejo de los DTOs del backend (serialización camelCase, enums como texto).

public sealed record GuestRequest(string DeviceId, string? DisplayName);
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record UpgradeRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds, ProfileDto Profile);

public sealed record LivesDto(int Current, int Max, int SecondsToNextLife);
public sealed record StreakDto(int Current, int Best, bool ActiveToday);
public sealed record TotalsDto(int Answered, int Correct, int Sessions, int Perfect, int DailyChallenges);
public sealed record ProfileDto(
    long Id, string DisplayName, string AvatarCode, bool IsGuest, string? Email,
    int Xp, int Level, int XpIntoLevel, int XpForNextLevel,
    int Coins, LivesDto Lives, StreakDto Streak, int WeeklyXp, TotalsDto Totals,
    bool IsPremium, DateTime? PremiumUntilUtc, bool ShowAds);
public sealed record UpdateProfileRequest(string? DisplayName, string? AvatarCode);

public sealed record CategoryDto(int Id, string Slug, string Name, string Emoji, string Color, string Description);
public sealed record ChoiceDto(int Id, string Text);
public sealed record QuestionDto(int Id, int CategoryId, string Type, int Difficulty, string Text, List<ChoiceDto> Choices);

public sealed record StartGameRequest(string Mode, int? CategoryId);
public sealed record StartGameResponse(Guid SessionId, List<QuestionDto> Questions, LivesDto Lives);
public sealed record SubmitAnswer(int QuestionId, int? ChoiceId);
public sealed record SubmitGameRequest(List<SubmitAnswer> Answers);
public sealed record AnswerResultDto(int QuestionId, int CorrectChoiceId, bool WasCorrect, string Explanation, string? FunFact);
public sealed record UnlockedAchievementDto(string Code, string Name, string Description, string Emoji, string Tier, int XpReward, int CoinReward);
public sealed record GameResultDto(
    int Correct, int Total, int XpEarned, int CoinsEarned, bool IsPerfect,
    bool LevelUp, int Level, StreakDto Streak,
    List<UnlockedAchievementDto> UnlockedAchievements,
    List<AnswerResultDto> Results,
    ProfileDto Profile);

public sealed record DailyStatusDto(DateOnly Date, bool Completed, int? Correct, int? Total, int? XpEarned);

public sealed record AchievementDto(
    string Code, string Name, string Description, string Emoji, string Tier,
    int XpReward, int CoinReward, int Threshold, string Criteria,
    int Progress, bool Unlocked, DateTime? UnlockedAtUtc);

public sealed record LeaderboardEntryDto(int Rank, string DisplayName, string AvatarCode, int WeeklyXp, int Level);
public sealed record LeaderboardDto(List<LeaderboardEntryDto> Top, int? MyRank, int MyWeeklyXp);

public sealed record StoreProductDto(string ProductId, string Name, string Description, int Lives, int Coins, string PriceHint);
public sealed record StoreCatalogDto(List<StoreProductDto> Products, int RefillCoinCost);
public sealed record PurchaseRequest(string Platform, string ProductId, string TransactionId, string Receipt);
public sealed record PurchaseResponse(int LivesGranted, int CoinsGranted, ProfileDto Profile);

public sealed record DeviceTokenRequest(string Platform, string Token);

// ---------- Anuncios recompensados ----------
public sealed record AdRewardResponse(LivesDto Lives, int RemainingToday, ProfileDto Profile);

// ---------- Minijuegos de entrenamiento ----------
public sealed record MinigameDto(string Code, string Name, string Emoji, string Description, int MaxXpPerSession);
public sealed record MinigameSubmitRequest(string Code, int Score, int DurationMs);
public sealed record MinigameResultDto(int XpEarned, int CoinsEarned, int DailyXpRemaining, StreakDto Streak, ProfileDto Profile);

/// <summary>Modos de juego que entiende el backend.</summary>
public static class GameModes
{
    public const string Quick = "Quick";
    public const string Category = "Category";
    public const string Daily = "Daily";
}
