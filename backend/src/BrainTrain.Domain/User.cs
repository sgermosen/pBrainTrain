namespace BrainTrain.Domain;

/// <summary>
/// Usuario del juego. El estado de gamificación "caliente" (vidas, XP, racha,
/// monedas) vive en esta misma fila para que el perfil completo se resuelva
/// con UNA sola lectura por clave primaria — crítico para servir 1M de
/// usuarios desde un VPS pequeño.
/// </summary>
public class User
{
    public long Id { get; set; }

    /// <summary>Null para cuentas de invitado.</summary>
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }

    /// <summary>Identificador de dispositivo para cuentas de invitado.</summary>
    public string? DeviceId { get; set; }
    public bool IsGuest { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Código de avatar predefinido (la app trae un set local, sin subida de fotos).</summary>
    public string AvatarCode { get; set; } = "fox";

    public DateTime CreatedAtUtc { get; set; }

    // ----- Progresión -----
    public int Xp { get; set; }
    public int Level { get; set; } = 1;
    public int Coins { get; set; }
    public int CoinsEarnedTotal { get; set; }

    // ----- Vidas (combustible). Regeneración perezosa: se calcula al leer. -----
    public int Lives { get; set; }
    public DateTime LivesUpdatedAtUtc { get; set; }

    // ----- Racha diaria -----
    public int StreakDays { get; set; }
    public int BestStreakDays { get; set; }
    public DateOnly? LastActivityDateUtc { get; set; }

    // ----- Leaderboard semanal (reinicio perezoso al cambiar de semana) -----
    public int WeeklyXp { get; set; }
    public DateOnly WeekStartUtc { get; set; }

    // ----- Membresía Premium (sin anuncios + conveniencia; nunca pay-to-win) -----
    public DateTime? PremiumUntilUtc { get; set; }

    // ----- Anuncios recompensados: tope diario de vidas por ver anuncios -----
    public DateOnly? AdRewardDateUtc { get; set; }
    public int AdRewardsToday { get; set; }

    // ----- Minijuegos de entrenamiento: tope diario de XP (anti-farmeo) -----
    public DateOnly? MinigameDateUtc { get; set; }
    public int MinigameXpToday { get; set; }

    // ----- Sesiones de enfoque (flow/respiración): XP simbólico con tope diario -----
    public DateOnly? FocusDateUtc { get; set; }
    public int FocusXpToday { get; set; }

    // ----- Contadores agregados para logros y estadísticas -----
    public int TotalAnswered { get; set; }
    public int TotalCorrect { get; set; }
    public int SessionsCompleted { get; set; }
    public int PerfectSessions { get; set; }
    public int DailyChallengesCompleted { get; set; }

    public ICollection<UserAchievement> Achievements { get; set; } = [];
}

public class RefreshToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }

    /// <summary>SHA-256 del token; el token en claro nunca se persiste.</summary>
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}

public class DeviceToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }
    public StorePlatform Platform { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}
