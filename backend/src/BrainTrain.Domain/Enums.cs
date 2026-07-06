namespace BrainTrain.Domain;

public enum QuestionType
{
    MultipleChoice = 0,
    TrueFalse = 1
}

public enum GameMode
{
    Quick = 0,
    Category = 1,
    Daily = 2
}

public enum AchievementTier
{
    Bronze = 0,
    Silver = 1,
    Gold = 2,
    Diamond = 3
}

/// <summary>Tipos de criterio soportados por el motor de logros.</summary>
public enum AchievementCriteria
{
    SessionsCompleted = 0,
    CorrectAnswers = 1,
    PerfectSessions = 2,
    StreakDays = 3,
    DailyChallengesCompleted = 4,
    CategoryCorrect = 5,
    LevelReached = 6,
    CoinsEarned = 7
}

public enum StorePlatform
{
    GooglePlay = 0,
    AppStore = 1,
    Test = 99
}
