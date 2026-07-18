using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

/// <summary>
/// Definición de un minijuego de entrenamiento. XP = score × XpNum / XpDen,
/// acotado por sesión y por día. Los topes existen para que el entrenamiento
/// complemente al quiz sin permitir farmear el leaderboard.
/// </summary>
public sealed record MinigameDef(
    string Code, string Name, string Emoji, string Description,
    int XpNum, int XpDen, int MaxXpPerSession, int MaxScore, int MinDurationMs)
{
    public int XpFor(int score) =>
        Math.Min(MaxXpPerSession, Math.Clamp(score, 0, MaxScore) * XpNum / XpDen);
}

public sealed class MinigameService(AppDbContext db, AchievementService achievements, IOptions<GameOptions> options)
{
    private readonly GameOptions _opt = options.Value;

    public static readonly IReadOnlyList<MinigameDef> Catalog =
    [
        new("g2048", "2048", "🔢",
            "Desliza y fusiona números iguales hasta llegar a 2048. Estrategia pura.",
            XpNum: 1, XpDen: 100, MaxXpPerSession: 80, MaxScore: 50000, MinDurationMs: 20_000),
        new("math_sprint", "Cálculo Rápido", "➗",
            "Resuelve todas las operaciones que puedas en 60 segundos.",
            XpNum: 2, XpDen: 1, MaxXpPerSession: 60, MaxScore: 60, MinDurationMs: 15_000),
        new("word_search", "Sopa de Letras", "🔤",
            "Encuentra las palabras escondidas en la cuadrícula.",
            XpNum: 6, XpDen: 1, MaxXpPerSession: 60, MaxScore: 12, MinDurationMs: 15_000),
        new("memory_pairs", "Parejas de Memoria", "🃏",
            "Voltea cartas y encuentra todas las parejas. Entrena tu memoria de trabajo.",
            XpNum: 5, XpDen: 1, MaxXpPerSession: 40, MaxScore: 8, MinDurationMs: 15_000),
        new("simon", "Simón Dice", "🚦",
            "Repite la secuencia de colores que crece sin parar. ¿Hasta dónde llega tu memoria?",
            XpNum: 4, XpDen: 1, MaxXpPerSession: 60, MaxScore: 30, MinDurationMs: 15_000),
        new("spot_diff", "Encuentra las Diferencias", "🔍",
            "Dos imágenes casi iguales: descubre las 5 diferencias de cada escena.",
            XpNum: 4, XpDen: 1, MaxXpPerSession: 60, MaxScore: 15, MinDurationMs: 15_000),
        new("rubik_guide", "Guía Cubo de Rubik", "🧊",
            "Aprende a armar el cubo paso a paso (método por capas) y cronometra tus tiempos.",
            XpNum: 20, XpDen: 1, MaxXpPerSession: 20, MaxScore: 1, MinDurationMs: 60_000),
    ];

    public static MinigameDef? Find(string code) => Catalog.FirstOrDefault(g => g.Code == code);

    public async Task<MinigameResultDto> SubmitAsync(long userId, MinigameSubmitRequest req, CancellationToken ct)
    {
        var game = Find(req.Code)
                   ?? throw new GameError(400, "unknown_minigame", "Minijuego desconocido.");
        if (req.Score < 0 || req.Score > game.MaxScore)
            throw new GameError(422, "bad_score", "Puntaje fuera de rango.");
        if (req.DurationMs < game.MinDurationMs)
            throw new GameError(422, "too_fast", "La sesión fue demasiado corta.");

        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Tope diario de XP por minijuegos (reinicio perezoso por fecha).
        if (user.MinigameDateUtc != today)
        {
            user.MinigameDateUtc = today;
            user.MinigameXpToday = 0;
        }
        var dailyRemaining = Math.Max(0, _opt.MinigameDailyXpCap - user.MinigameXpToday);
        var xp = Math.Min(game.XpFor(req.Score), dailyRemaining);
        var coins = xp / 10;

        ProgressionLogic.EnsureCurrentWeek(user, today);
        ProgressionLogic.UpdateStreak(user, today); // entrenar también cuenta para la racha

        var levelBefore = user.Level;
        user.MinigameXpToday += xp;
        user.Xp += xp;
        user.WeeklyXp += xp;
        user.Coins += coins;
        user.CoinsEarnedTotal += coins;
        user.Level = ProgressionLogic.LevelForXp(user.Xp);
        if (user.Level > levelBefore)
        {
            var reward = _opt.LevelUpCoins * (user.Level - levelBefore);
            user.Coins += reward;
            user.CoinsEarnedTotal += reward;
            coins += reward;
        }

        await achievements.EvaluateAsync(user, ct);
        await db.SaveChangesAsync(ct);

        return new MinigameResultDto(
            xp, coins,
            Math.Max(0, _opt.MinigameDailyXpCap - user.MinigameXpToday),
            new StreakDto(user.StreakDays, user.BestStreakDays, user.LastActivityDateUtc == today),
            ProfileMapper.ToDto(user, _opt, now));
    }
}
