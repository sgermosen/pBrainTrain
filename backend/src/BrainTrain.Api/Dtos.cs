using System.Text.Json.Serialization;
using BrainTrain.Domain;

namespace BrainTrain.Api;

// ---------- Auth ----------
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record GuestRequest(string DeviceId, string? DisplayName);
public sealed record RefreshRequest(string RefreshToken);
public sealed record UpgradeRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds, ProfileDto Profile);

// ---------- Perfil ----------
public sealed record LivesDto(int Current, int Max, int SecondsToNextLife);
public sealed record StreakDto(int Current, int Best, bool ActiveToday);
public sealed record TotalsDto(int Answered, int Correct, int Sessions, int Perfect, int DailyChallenges);
public sealed record ProfileDto(
    long Id, string DisplayName, string AvatarCode, bool IsGuest, string? Email,
    int Xp, int Level, int XpIntoLevel, int XpForNextLevel,
    int Coins, LivesDto Lives, StreakDto Streak, int WeeklyXp, TotalsDto Totals,
    bool IsPremium, DateTime? PremiumUntilUtc, bool ShowAds);
public sealed record UpdateProfileRequest(string? DisplayName, string? AvatarCode);

// ---------- Contenido ----------
public sealed record CategoryDto(int Id, string Slug, string Name, string Emoji, string Color, string Description);
public sealed record ChoiceDto(int Id, string Text);
public sealed record QuestionDto(int Id, int CategoryId, QuestionType Type, int Difficulty, string Text, IReadOnlyList<ChoiceDto> Choices);

// ---------- Juego ----------
public sealed record StartGameRequest(GameMode Mode, int? CategoryId);
public sealed record StartGameResponse(Guid SessionId, IReadOnlyList<QuestionDto> Questions, LivesDto Lives);
public sealed record SubmitAnswer(int QuestionId, int? ChoiceId);
public sealed record SubmitGameRequest(IReadOnlyList<SubmitAnswer> Answers);
public sealed record AnswerResultDto(int QuestionId, int CorrectChoiceId, bool WasCorrect, string Explanation, string? FunFact);
public sealed record UnlockedAchievementDto(string Code, string Name, string Description, string Emoji, AchievementTier Tier, int XpReward, int CoinReward);
public sealed record GameResultDto(
    int Correct, int Total, int XpEarned, int CoinsEarned, bool IsPerfect,
    bool LevelUp, int Level, StreakDto Streak,
    IReadOnlyList<UnlockedAchievementDto> UnlockedAchievements,
    IReadOnlyList<AnswerResultDto> Results,
    ProfileDto Profile);

// ---------- Reto diario ----------
public sealed record DailyStatusDto(DateOnly Date, bool Completed, int? Correct, int? Total, int? XpEarned, Guid? SessionId, IReadOnlyList<QuestionDto>? Questions);

// ---------- Logros ----------
public sealed record AchievementDto(
    string Code, string Name, string Description, string Emoji, AchievementTier Tier,
    int XpReward, int CoinReward, int Threshold, AchievementCriteria Criteria,
    int Progress, bool Unlocked, DateTime? UnlockedAtUtc);

// ---------- Leaderboard ----------
public sealed record LeaderboardEntryDto(int Rank, string DisplayName, string AvatarCode, int WeeklyXp, int Level);
public sealed record LeaderboardDto(IReadOnlyList<LeaderboardEntryDto> Top, int? MyRank, int MyWeeklyXp);

// ---------- Tienda ----------
public sealed record StoreProductDto(string ProductId, string Name, string Description, int Lives, int Coins, string PriceHint);
public sealed record StoreCatalogDto(IReadOnlyList<StoreProductDto> Products, int RefillCoinCost);
public sealed record PurchaseRequest(StorePlatform Platform, string ProductId, string TransactionId, string Receipt);
public sealed record PurchaseResponse(int LivesGranted, int CoinsGranted, ProfileDto Profile);

// ---------- Dispositivos ----------
public sealed record DeviceTokenRequest(StorePlatform Platform, string Token);

// ---------- Anuncios recompensados ----------
public sealed record AdRewardResponse(LivesDto Lives, int RemainingToday, ProfileDto Profile);

// ---------- Minijuegos de entrenamiento ----------
public sealed record MinigameDto(string Code, string Name, string Emoji, string Description, int MaxXpPerSession);
public sealed record MinigameSubmitRequest(string Code, int Score, int DurationMs);
public sealed record MinigameResultDto(int XpEarned, int CoinsEarned, int DailyXpRemaining, StreakDto Streak, ProfileDto Profile);

// ---------- Sesiones de enfoque ----------
public sealed record FocusCompleteRequest(string Kind, int Seconds);
public sealed record FocusResultDto(int XpEarned, int DailyXpRemaining, StreakDto Streak, ProfileDto Profile);

// ---------- PayPal (portal web) ----------
public sealed record PayPalConfigDto(bool Enabled, string ClientId, string Currency, string Mode);
public sealed record PayPalCreateOrderRequest(string ProductId);
public sealed record PayPalCreateOrderResponse(string OrderId);
public sealed record PayPalCaptureRequest(string OrderId);

// ---------- Salud ----------
public sealed record ApiInfoDto(string Name, string Version, string Environment);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(GuestRequest))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(UpgradeRequest))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(ProfileDto))]
[JsonSerializable(typeof(UpdateProfileRequest))]
[JsonSerializable(typeof(LivesDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
[JsonSerializable(typeof(StartGameRequest))]
[JsonSerializable(typeof(StartGameResponse))]
[JsonSerializable(typeof(SubmitGameRequest))]
[JsonSerializable(typeof(GameResultDto))]
[JsonSerializable(typeof(DailyStatusDto))]
[JsonSerializable(typeof(List<AchievementDto>))]
[JsonSerializable(typeof(LeaderboardDto))]
[JsonSerializable(typeof(StoreCatalogDto))]
[JsonSerializable(typeof(PurchaseRequest))]
[JsonSerializable(typeof(PurchaseResponse))]
[JsonSerializable(typeof(DeviceTokenRequest))]
[JsonSerializable(typeof(AdRewardResponse))]
[JsonSerializable(typeof(List<MinigameDto>))]
[JsonSerializable(typeof(MinigameSubmitRequest))]
[JsonSerializable(typeof(MinigameResultDto))]
[JsonSerializable(typeof(FocusCompleteRequest))]
[JsonSerializable(typeof(FocusResultDto))]
[JsonSerializable(typeof(PayPalConfigDto))]
[JsonSerializable(typeof(PayPalCreateOrderRequest))]
[JsonSerializable(typeof(PayPalCreateOrderResponse))]
[JsonSerializable(typeof(PayPalCaptureRequest))]
[JsonSerializable(typeof(ApiInfoDto))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
public partial class ApiJsonContext : JsonSerializerContext;
