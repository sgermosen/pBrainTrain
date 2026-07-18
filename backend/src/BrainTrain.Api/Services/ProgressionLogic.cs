using BrainTrain.Domain;

namespace BrainTrain.Api.Services;

/// <summary>
/// Reglas puras de progresión (sin IO): curva de niveles, vidas con
/// regeneración perezosa, rachas y semanas de leaderboard.
/// Mantenerlas puras permite testearlas de forma exhaustiva y barata.
/// </summary>
public static class ProgressionLogic
{
    /// <summary>XP total acumulado necesario para alcanzar el nivel <paramref name="level"/> (nivel 1 = 0 XP).</summary>
    public static int XpForLevel(int level) => 50 * (level - 1) * level;

    /// <summary>Nivel correspondiente a un XP acumulado. Curva suave: 100, 300, 600, 1000…</summary>
    public static int LevelForXp(int xp)
    {
        if (xp <= 0) return 1;
        return (int)Math.Floor((50d + Math.Sqrt(2500d + 200d * xp)) / 100d);
    }

    /// <summary>
    /// Calcula las vidas efectivas de un usuario en <paramref name="nowUtc"/> aplicando
    /// regeneración perezosa (1 vida cada <paramref name="regenMinutes"/> hasta <paramref name="maxLives"/>).
    /// Devuelve además el nuevo "ancla" temporal y los segundos hasta la próxima vida.
    /// Las vidas compradas pueden superar el máximo: por encima del tope no se regenera ni caduca.
    /// </summary>
    public static (int Lives, DateTime AnchorUtc, int SecondsToNext) ComputeLives(
        int storedLives, DateTime storedAnchorUtc, DateTime nowUtc, int maxLives, int regenMinutes)
    {
        if (storedLives >= maxLives)
            return (storedLives, nowUtc, 0);

        var regen = TimeSpan.FromMinutes(regenMinutes);
        var elapsed = nowUtc - storedAnchorUtc;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        var gained = (int)(elapsed.Ticks / regen.Ticks);
        var lives = Math.Min(maxLives, storedLives + gained);
        if (lives >= maxLives)
            return (lives, nowUtc, 0);

        var leftover = TimeSpan.FromTicks(elapsed.Ticks % regen.Ticks);
        var anchor = nowUtc - leftover;
        var secondsToNext = (int)Math.Ceiling((regen - leftover).TotalSeconds);
        return (lives, anchor, secondsToNext);
    }

    /// <summary>¿La membresía Premium está activa en este momento?</summary>
    public static bool IsPremium(User user, DateTime nowUtc) =>
        user.PremiumUntilUtc is { } until && until > nowUtc;

    /// <summary>Parámetros de vidas efectivos según membresía (Premium: más tope y regeneración más rápida).</summary>
    public static (int MaxLives, int RegenMinutes) LivesParams(User user, GameOptions opt, DateTime nowUtc) =>
        IsPremium(user, nowUtc)
            ? (opt.PremiumMaxLives, opt.PremiumLifeRegenMinutes)
            : (opt.MaxLives, opt.LifeRegenMinutes);

    /// <summary>Lunes (UTC) de la semana a la que pertenece <paramref name="dateUtc"/>.</summary>
    public static DateOnly WeekStart(DateOnly dateUtc)
    {
        var diff = ((int)dateUtc.DayOfWeek + 6) % 7; // lunes=0 … domingo=6
        return dateUtc.AddDays(-diff);
    }

    /// <summary>
    /// Actualiza la racha del usuario por actividad completada hoy.
    /// Devuelve true si la racha creció (para celebración en la app).
    /// </summary>
    public static bool UpdateStreak(User user, DateOnly todayUtc)
    {
        if (user.LastActivityDateUtc == todayUtc)
            return false;

        user.StreakDays = user.LastActivityDateUtc == todayUtc.AddDays(-1)
            ? user.StreakDays + 1
            : 1;
        user.BestStreakDays = Math.Max(user.BestStreakDays, user.StreakDays);
        user.LastActivityDateUtc = todayUtc;
        return true;
    }

    // ----- Ligas por divisiones (umbral de XP semanal, evaluación perezosa) -----
    public static readonly string[] LeagueNames = ["Bronce", "Plata", "Oro", "Diamante", "Leyenda"];
    /// <summary>XP semanal necesario para SUBIR desde el tier i.</summary>
    public static readonly int[] LeaguePromoteXp = [150, 300, 500, 800];
    /// <summary>XP semanal mínimo para NO BAJAR estando en el tier i.</summary>
    public static readonly int[] LeagueStayXp = [0, 50, 100, 150, 200];

    /// <summary>
    /// Reinicio perezoso del contador semanal. Al cerrar una semana se evalúa
    /// la liga con el XP logrado: sin cohortes ni jobs — reglas claras tipo
    /// "gana 300 XP esta semana para subir a Oro".
    /// </summary>
    public static void EnsureCurrentWeek(User user, DateOnly todayUtc)
    {
        var week = WeekStart(todayUtc);
        if (user.WeekStartUtc == week) return;

        var xp = user.WeeklyXp;
        if (user.LeagueTier < LeaguePromoteXp.Length && xp >= LeaguePromoteXp[user.LeagueTier])
            user.LeagueTier++;
        else if (user.LeagueTier > 0 && xp < LeagueStayXp[user.LeagueTier])
            user.LeagueTier--;

        user.WeekStartUtc = week;
        user.WeeklyXp = 0;
    }

    /// <summary>Reinicio perezoso de los contadores de misiones diarias.</summary>
    public static void EnsureQuestDay(User user, DateOnly todayUtc)
    {
        if (user.QuestDateUtc == todayUtc) return;
        user.QuestDateUtc = todayUtc;
        user.SessionsToday = 0;
        user.PerfectsToday = 0;
        user.CorrectToday = 0;
        user.MinigamesToday = 0;
        user.QuestClaimedMask = 0;
    }
}
