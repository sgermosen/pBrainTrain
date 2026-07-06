using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

public sealed class GameError(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public sealed class GameService(
    AppDbContext db,
    QuestionCatalog catalog,
    AchievementService achievements,
    IOptions<GameOptions> options)
{
    private readonly GameOptions _opt = options.Value;

    // ---------------------------------------------------------------- start
    public async Task<StartGameResponse> StartAsync(long userId, StartGameRequest req, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");

        var now = DateTime.UtcNow;
        var snap = await catalog.GetAsync(ct);

        if (req.Mode == GameMode.Daily)
            return await StartDailyAsync(user, snap, now, ct);

        // Las partidas normales consumen 1 vida (combustible).
        var (lives, anchor, _) = ProgressionLogic.ComputeLives(
            user.Lives, user.LivesUpdatedAtUtc, now, _opt.MaxLives, _opt.LifeRegenMinutes);
        if (lives <= 0)
            throw new GameError(409, "no_lives", "Sin vidas. Espera la recarga, canjea monedas o visita la tienda.");

        user.Lives = lives - 1;
        user.LivesUpdatedAtUtc = user.Lives >= _opt.MaxLives ? now : anchor;

        int[] pool;
        if (req.Mode == GameMode.Category)
        {
            if (req.CategoryId is null || !snap.QuestionIdsByCategory.TryGetValue(req.CategoryId.Value, out pool!))
                throw new GameError(400, "bad_category", "Categoría inválida.");
        }
        else
        {
            pool = snap.AllQuestionIds;
        }

        var difficultyCenter = await AdaptiveDifficultyCenterAsync(user.Id, req.CategoryId, ct);
        var ids = PickQuestions(snap, pool, _opt.QuestionsPerSession, difficultyCenter, Random.Shared);

        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Mode = req.Mode,
            CategoryId = req.CategoryId,
            QuestionIdsCsv = string.Join(',', ids),
            StartedAtUtc = now,
            TotalCount = ids.Length
        };
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(ct);

        var (livesNow, _, secs) = ProgressionLogic.ComputeLives(
            user.Lives, user.LivesUpdatedAtUtc, now, _opt.MaxLives, _opt.LifeRegenMinutes);

        return new StartGameResponse(
            session.Id,
            ids.Select(i => snap.QuestionsById[i].ToDto()).ToList(),
            new LivesDto(livesNow, _opt.MaxLives, secs));
    }

    private async Task<StartGameResponse> StartDailyAsync(User user, CatalogSnapshot snap, DateTime now, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(now);
        var alreadyDone = await db.DailyChallengeEntries.AsNoTracking()
            .AnyAsync(d => d.UserId == user.Id && d.DateUtc == today, ct);
        if (alreadyDone)
            throw new GameError(409, "daily_done", "Ya completaste el reto de hoy. ¡Vuelve mañana!");

        // Reutiliza la sesión abierta de hoy si existe (evita farmear reinicios).
        var startOfDay = now.Date;
        var open = await db.GameSessions
            .Where(s => s.UserId == user.Id && s.Mode == GameMode.Daily
                        && s.CompletedAtUtc == null && s.StartedAtUtc >= startOfDay)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        int[] ids;
        GameSession session;
        if (open is not null)
        {
            session = open;
            ids = ParseIds(open.QuestionIdsCsv);
        }
        else
        {
            ids = DailyQuestionIds(snap, today, _opt.DailyQuestions);
            session = new GameSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Mode = GameMode.Daily,
                QuestionIdsCsv = string.Join(',', ids),
                StartedAtUtc = now,
                TotalCount = ids.Length
            };
            db.GameSessions.Add(session);
            await db.SaveChangesAsync(ct);
        }

        var (livesNow, _, secs) = ProgressionLogic.ComputeLives(
            user.Lives, user.LivesUpdatedAtUtc, now, _opt.MaxLives, _opt.LifeRegenMinutes);

        return new StartGameResponse(
            session.Id,
            ids.Select(i => snap.QuestionsById[i].ToDto()).ToList(),
            new LivesDto(livesNow, _opt.MaxLives, secs));
    }

    // --------------------------------------------------------------- submit
    public async Task<GameResultDto> SubmitAsync(long userId, Guid sessionId, SubmitGameRequest req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var session = await db.GameSessions
                          .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
                      ?? throw new GameError(404, "session_not_found", "Partida no encontrada.");

        if (session.CompletedAtUtc is not null)
            throw new GameError(409, "already_submitted", "Esta partida ya fue enviada.");
        if (now - session.StartedAtUtc > TimeSpan.FromMinutes(_opt.SessionExpiryMinutes))
            throw new GameError(410, "session_expired", "La partida expiró. Inicia una nueva.");
        if (now - session.StartedAtUtc < TimeSpan.FromSeconds(_opt.MinSessionSeconds))
            throw new GameError(422, "too_fast", "Respuestas demasiado rápidas.");

        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");

        var snap = await catalog.GetAsync(ct);
        var servedIds = ParseIds(session.QuestionIdsCsv);
        var servedSet = servedIds.ToHashSet();
        var answersByQuestion = new Dictionary<int, int?>();
        foreach (var a in req.Answers)
        {
            // Solo cuentan respuestas a preguntas realmente servidas, una vez cada una.
            if (servedSet.Contains(a.QuestionId))
                answersByQuestion.TryAdd(a.QuestionId, a.ChoiceId);
        }

        // ------ Corrección en servidor (las respuestas correctas nunca viajan al cliente antes) ------
        var results = new List<AnswerResultDto>(servedIds.Length);
        var correct = 0;
        var xpFromAnswers = 0;
        var correctByCategory = new Dictionary<int, (int Correct, int Answered)>();

        foreach (var qid in servedIds)
        {
            if (!snap.QuestionsById.TryGetValue(qid, out var q))
                continue; // pregunta desactivada entre inicio y envío: se omite

            answersByQuestion.TryGetValue(qid, out var choiceId);
            var wasCorrect = choiceId is not null && choiceId == q.CorrectChoiceId;
            if (wasCorrect)
            {
                correct++;
                xpFromAnswers += _opt.XpPerDifficulty * q.Difficulty;
            }

            var stat = correctByCategory.GetValueOrDefault(q.CategoryId);
            correctByCategory[q.CategoryId] = (stat.Correct + (wasCorrect ? 1 : 0), stat.Answered + 1);

            results.Add(new AnswerResultDto(qid, q.CorrectChoiceId, wasCorrect, q.Explanation, q.FunFact));
        }

        var total = results.Count;
        var isPerfect = total > 0 && correct == total;
        var isDaily = session.Mode == GameMode.Daily;

        var xp = xpFromAnswers;
        var coins = _opt.SessionCompleteCoins;
        if (isPerfect) { xp += _opt.PerfectBonusXp; coins += _opt.PerfectBonusCoins; }
        if (isDaily) { xp += _opt.DailyBonusXp; coins += _opt.DailyBonusCoins; }

        var today = DateOnly.FromDateTime(now);
        if (isDaily)
        {
            var duplicate = await db.DailyChallengeEntries.AsNoTracking()
                .AnyAsync(d => d.UserId == userId && d.DateUtc == today, ct);
            if (duplicate)
                throw new GameError(409, "daily_done", "Ya completaste el reto de hoy.");

            db.DailyChallengeEntries.Add(new DailyChallengeEntry
            {
                UserId = userId,
                DateUtc = today,
                TotalCount = total,
                CorrectCount = correct,
                XpEarned = xp,
                CompletedAtUtc = now
            });
            user.DailyChallengesCompleted++;
        }

        // ------ Progresión del usuario (todo en una sola transacción/SaveChanges) ------
        var levelBefore = user.Level;
        ProgressionLogic.EnsureCurrentWeek(user, today);
        ProgressionLogic.UpdateStreak(user, today);

        user.TotalAnswered += total;
        user.TotalCorrect += correct;
        user.SessionsCompleted++;
        if (isPerfect) user.PerfectSessions++;
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

        await UpsertCategoryStatsAsync(userId, correctByCategory, ct);

        session.CompletedAtUtc = now;
        session.CorrectCount = correct;
        session.XpEarned = xp;
        session.CoinsEarned = coins;
        session.IsPerfect = isPerfect;

        var unlocked = await achievements.EvaluateAsync(user, ct);

        await db.SaveChangesAsync(ct);

        return new GameResultDto(
            correct, total, xp, coins, isPerfect,
            user.Level > levelBefore, user.Level,
            new StreakDto(user.StreakDays, user.BestStreakDays, user.LastActivityDateUtc == today),
            unlocked, results, ProfileMapper.ToDto(user, _opt, now));
    }

    // -------------------------------------------------------------- helpers
    internal static int[] ParseIds(string csv) =>
        csv.Length == 0 ? [] : csv.Split(',').Select(int.Parse).ToArray();

    /// <summary>
    /// Centro de dificultad adaptativa (1..5) según la precisión del usuario:
    /// empieza fácil y sube a medida que domina — refuerzo positivo, no frustración.
    /// </summary>
    private async Task<double> AdaptiveDifficultyCenterAsync(long userId, int? categoryId, CancellationToken ct)
    {
        var q = db.UserCategoryStats.AsNoTracking().Where(s => s.UserId == userId);
        if (categoryId is not null)
            q = q.Where(s => s.CategoryId == categoryId);

        var agg = await q.GroupBy(_ => 1)
            .Select(g => new { Answered = g.Sum(s => s.Answered), Correct = g.Sum(s => s.Correct) })
            .FirstOrDefaultAsync(ct);

        if (agg is null || agg.Answered < 10) return 1.5;
        var acc = (double)agg.Correct / agg.Answered;
        return acc switch
        {
            < 0.40 => 1.5,
            < 0.60 => 2.2,
            < 0.75 => 3.0,
            < 0.88 => 3.8,
            _ => 4.4
        };
    }

    /// <summary>Muestreo aleatorio ponderado hacia la dificultad objetivo.</summary>
    internal static int[] PickQuestions(CatalogSnapshot snap, int[] pool, int count, double difficultyCenter, Random rng)
    {
        if (pool.Length <= count)
            return [.. pool.OrderBy(_ => rng.Next())];

        // Peso gaussiano alrededor del centro de dificultad; sigma amplio para variedad.
        var weighted = pool.Select(id =>
        {
            var d = snap.QuestionsById[id].Difficulty;
            var w = Math.Exp(-Math.Pow(d - difficultyCenter, 2) / (2 * 1.6 * 1.6));
            // "Sorteo exponencial" ponderado: claves aleatorias ~ Exp(w), tomamos las mayores.
            var key = Math.Pow(rng.NextDouble(), 1.0 / Math.Max(w, 1e-6));
            return (id, key);
        });
        return weighted.OrderByDescending(x => x.key).Take(count).Select(x => x.id).ToArray();
    }

    /// <summary>Set determinista del reto diario: igual para todos los jugadores del día.</summary>
    internal static int[] DailyQuestionIds(CatalogSnapshot snap, DateOnly date, int count)
    {
        var rng = new Random(date.DayNumber * 2654435761u.GetHashCode() ^ 0x5EED);
        var sorted = snap.AllQuestionIds.OrderBy(i => i).ToArray();
        return sorted.OrderBy(_ => rng.Next()).Take(Math.Min(count, sorted.Length)).ToArray();
    }

    private async Task UpsertCategoryStatsAsync(
        long userId, Dictionary<int, (int Correct, int Answered)> delta, CancellationToken ct)
    {
        if (delta.Count == 0) return;
        var ids = delta.Keys.ToArray();
        var existing = await db.UserCategoryStats
            .Where(s => s.UserId == userId && ids.Contains(s.CategoryId))
            .ToDictionaryAsync(s => s.CategoryId, ct);

        foreach (var (categoryId, d) in delta)
        {
            if (existing.TryGetValue(categoryId, out var stat))
            {
                stat.Answered += d.Answered;
                stat.Correct += d.Correct;
            }
            else
            {
                db.UserCategoryStats.Add(new UserCategoryStat
                {
                    UserId = userId,
                    CategoryId = categoryId,
                    Answered = d.Answered,
                    Correct = d.Correct
                });
            }
        }
    }
}
