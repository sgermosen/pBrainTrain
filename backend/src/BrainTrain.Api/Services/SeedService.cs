using System.Text.Json;
using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BrainTrain.Api.Services;

/// <summary>
/// Carga el contenido inicial (categorías, preguntas, logros) desde los JSON
/// de Data/Seed la primera vez que arranca contra una base vacía.
/// </summary>
public static class SeedService
{
    private sealed record SeedCategory(string Slug, string Name, string Emoji, string Color, string Description);
    private sealed record SeedChoice(string Text, bool IsCorrect);
    private sealed record SeedQuestion(string Category, string Type, int Difficulty, string Text,
        List<SeedChoice> Choices, string Explanation, string? FunFact);
    private sealed record SeedQuestionsFile(List<SeedCategory> Categories, List<SeedQuestion> Questions);
    private sealed record SeedAchievement(string Code, string Name, string Description, string Emoji,
        string Tier, int XpReward, int CoinReward, string CriteriaType, int Threshold, string? CategorySlug);
    private sealed record SeedAchievementsFile(List<SeedAchievement> Achievements);

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static async Task SeedAsync(AppDbContext db, string contentRoot, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Categories.AnyAsync(ct))
            return;

        var seedDir = Path.Combine(contentRoot, "Data", "Seed");
        var questionsPath = Path.Combine(seedDir, "questions.es.json");
        var achievementsPath = Path.Combine(seedDir, "achievements.es.json");

        if (!File.Exists(questionsPath))
        {
            logger.LogWarning("No se encontró {Path}; base de datos sin contenido inicial", questionsPath);
            return;
        }

        var qFile = JsonSerializer.Deserialize<SeedQuestionsFile>(await File.ReadAllTextAsync(questionsPath, ct), JsonOpts)
                    ?? throw new InvalidOperationException("questions.es.json inválido");

        var categories = new Dictionary<string, Category>();
        var sort = 0;
        foreach (var c in qFile.Categories)
        {
            var cat = new Category
            {
                Slug = c.Slug, Name = c.Name, Emoji = c.Emoji, Color = c.Color,
                Description = c.Description, SortOrder = sort++, IsActive = true
            };
            categories[c.Slug] = cat;
            db.Categories.Add(cat);
        }

        foreach (var q in qFile.Questions)
        {
            if (!categories.TryGetValue(q.Category, out var cat))
            {
                logger.LogWarning("Pregunta con categoría desconocida '{Slug}' omitida", q.Category);
                continue;
            }
            var question = new Question
            {
                Category = cat,
                Type = q.Type == "true_false" ? QuestionType.TrueFalse : QuestionType.MultipleChoice,
                Difficulty = Math.Clamp(q.Difficulty, 1, 5),
                Text = q.Text,
                Explanation = q.Explanation,
                FunFact = q.FunFact,
                IsActive = true
            };
            var order = 0;
            foreach (var ch in q.Choices)
                question.Choices.Add(new Choice { Text = ch.Text, IsCorrect = ch.IsCorrect, SortOrder = order++ });
            db.Questions.Add(question);
        }

        if (File.Exists(achievementsPath))
        {
            var aFile = JsonSerializer.Deserialize<SeedAchievementsFile>(await File.ReadAllTextAsync(achievementsPath, ct), JsonOpts)
                        ?? throw new InvalidOperationException("achievements.es.json inválido");
            foreach (var a in aFile.Achievements)
            {
                db.Achievements.Add(new Achievement
                {
                    Code = a.Code,
                    Name = a.Name,
                    Description = a.Description,
                    Emoji = a.Emoji,
                    Tier = Enum.Parse<AchievementTier>(a.Tier, ignoreCase: true),
                    XpReward = a.XpReward,
                    CoinReward = a.CoinReward,
                    CriteriaType = ParseCriteria(a.CriteriaType),
                    Threshold = a.Threshold,
                    CategoryId = null // se resuelve tras guardar categorías, abajo
                });
            }

            await db.SaveChangesAsync(ct);

            // Resuelve CategorySlug → CategoryId para logros de maestría.
            var byCode = await db.Achievements.ToDictionaryAsync(x => x.Code, ct);
            foreach (var a in aFile.Achievements.Where(x => !string.IsNullOrEmpty(x.CategorySlug)))
            {
                if (byCode.TryGetValue(a.Code, out var ach) && categories.TryGetValue(a.CategorySlug!, out var cat))
                    ach.CategoryId = cat.Id;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seed completado: {Cats} categorías, {Qs} preguntas, {As} logros",
            await db.Categories.CountAsync(ct), await db.Questions.CountAsync(ct), await db.Achievements.CountAsync(ct));
    }

    private static AchievementCriteria ParseCriteria(string s) => s switch
    {
        "sessions_completed" => AchievementCriteria.SessionsCompleted,
        "correct_answers" => AchievementCriteria.CorrectAnswers,
        "perfect_sessions" => AchievementCriteria.PerfectSessions,
        "streak_days" => AchievementCriteria.StreakDays,
        "daily_challenges_completed" => AchievementCriteria.DailyChallengesCompleted,
        "category_correct" => AchievementCriteria.CategoryCorrect,
        "level_reached" => AchievementCriteria.LevelReached,
        "coins_earned" => AchievementCriteria.CoinsEarned,
        _ => throw new InvalidOperationException($"criteriaType desconocido: {s}")
    };
}
