using BrainTrain.Api.Services;
using BrainTrain.Domain;

namespace BrainTrain.Tests;

public class ProgressionLogicTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 100)]
    [InlineData(3, 300)]
    [InlineData(4, 600)]
    [InlineData(5, 1000)]
    public void XpForLevel_SigueLaCurva(int level, int xp) =>
        Assert.Equal(xp, ProgressionLogic.XpForLevel(level));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(299, 2)]
    [InlineData(300, 3)]
    [InlineData(1000, 5)]
    [InlineData(123456, 50)]
    public void LevelForXp_EsInversaDeLaCurva(int xp, int expected)
    {
        Assert.Equal(expected, ProgressionLogic.LevelForXp(xp));
        // Coherencia: el XP del nivel devuelto nunca supera el XP dado.
        Assert.True(ProgressionLogic.XpForLevel(expected) <= xp);
        Assert.True(ProgressionLogic.XpForLevel(expected + 1) > xp);
    }

    [Fact]
    public void ComputeLives_RegeneraUnaVidaPorPeriodo()
    {
        var anchor = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var (lives, _, secs) = ProgressionLogic.ComputeLives(2, anchor, anchor.AddMinutes(65), maxLives: 5, regenMinutes: 30);
        Assert.Equal(4, lives);
        // Sobraron 5 min del ciclo: faltan 25 para la siguiente vida.
        Assert.Equal(25 * 60, secs);
    }

    [Fact]
    public void ComputeLives_NoSuperaElMaximoPorRegeneracion()
    {
        var anchor = DateTime.UtcNow.AddHours(-10);
        var (lives, _, secs) = ProgressionLogic.ComputeLives(1, anchor, DateTime.UtcNow, 5, 30);
        Assert.Equal(5, lives);
        Assert.Equal(0, secs);
    }

    [Fact]
    public void ComputeLives_VidasCompradasPorEncimaDelMaximoSeConservan()
    {
        var now = DateTime.UtcNow;
        var (lives, _, secs) = ProgressionLogic.ComputeLives(12, now.AddHours(-5), now, 5, 30);
        Assert.Equal(12, lives);
        Assert.Equal(0, secs);
    }

    [Theory]
    [InlineData("2026-07-06", "2026-07-06")] // lunes
    [InlineData("2026-07-09", "2026-07-06")] // jueves
    [InlineData("2026-07-12", "2026-07-06")] // domingo
    public void WeekStart_SiempreLunes(string date, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), ProgressionLogic.WeekStart(DateOnly.Parse(date)));

    [Fact]
    public void UpdateStreak_CreceSoloUnaVezPorDia()
    {
        var user = new User();
        var today = new DateOnly(2026, 7, 6);

        Assert.True(ProgressionLogic.UpdateStreak(user, today));
        Assert.Equal(1, user.StreakDays);
        Assert.False(ProgressionLogic.UpdateStreak(user, today)); // mismo día: no crece
        Assert.Equal(1, user.StreakDays);

        Assert.True(ProgressionLogic.UpdateStreak(user, today.AddDays(1)));
        Assert.Equal(2, user.StreakDays);

        // Se rompe la racha si faltó un día.
        Assert.True(ProgressionLogic.UpdateStreak(user, today.AddDays(3)));
        Assert.Equal(1, user.StreakDays);
        Assert.Equal(2, user.BestStreakDays);
    }

    [Fact]
    public void EnsureCurrentWeek_ReiniciaAlCambiarDeSemana()
    {
        var user = new User { WeeklyXp = 500, WeekStartUtc = new DateOnly(2026, 6, 29) };
        ProgressionLogic.EnsureCurrentWeek(user, new DateOnly(2026, 7, 8));
        Assert.Equal(0, user.WeeklyXp);
        Assert.Equal(new DateOnly(2026, 7, 6), user.WeekStartUtc);
    }
}
