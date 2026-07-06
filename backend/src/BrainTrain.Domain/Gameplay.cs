namespace BrainTrain.Domain;

/// <summary>
/// Una partida. Las preguntas servidas se guardan como CSV de IDs en la misma
/// fila (en vez de una tabla detalle) para minimizar escrituras e IO: una fila
/// por partida es lo que permite escalar en hardware modesto.
/// </summary>
public class GameSession
{
    public Guid Id { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }

    public GameMode Mode { get; set; }
    public int? CategoryId { get; set; }

    /// <summary>IDs de preguntas servidas, en orden, separadas por coma.</summary>
    public string QuestionIdsCsv { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public int TotalCount { get; set; }
    public int CorrectCount { get; set; }
    public int XpEarned { get; set; }
    public int CoinsEarned { get; set; }
    public bool IsPerfect { get; set; }
}

/// <summary>
/// Estadística agregada por usuario y categoría (para logros de maestría y
/// dificultad adaptativa) — evita almacenar cada respuesta individual, que a
/// 1M de usuarios sería una tabla de miles de millones de filas.
/// </summary>
public class UserCategoryStat
{
    public long UserId { get; set; }
    public int CategoryId { get; set; }
    public int Answered { get; set; }
    public int Correct { get; set; }
}

/// <summary>Resultado del reto diario; PK compuesta (usuario, fecha) impide repetirlo.</summary>
public class DailyChallengeEntry
{
    public long UserId { get; set; }
    public DateOnly DateUtc { get; set; }
    public int TotalCount { get; set; }
    public int CorrectCount { get; set; }
    public int XpEarned { get; set; }
    public DateTime CompletedAtUtc { get; set; }
}
