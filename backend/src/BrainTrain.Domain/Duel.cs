namespace BrainTrain.Domain;

/// <summary>
/// Duelo asíncrono 1v1: ambos jugadores responden las MISMAS preguntas y se
/// comparan puntajes. Se une por código de 6 letras o por emparejamiento
/// aleatorio (duelos marcados abiertos al público).
/// </summary>
public class Duel
{
    public Guid Id { get; set; }

    /// <summary>Código corto para compartir (ej. "K3PXQ7").</summary>
    public string Code { get; set; } = string.Empty;

    public long ChallengerUserId { get; set; }
    public User? Challenger { get; set; }
    public long? OpponentUserId { get; set; }
    public User? Opponent { get; set; }

    /// <summary>Preguntas compartidas por ambos jugadores.</summary>
    public string QuestionIdsCsv { get; set; } = string.Empty;

    public int? ChallengerScore { get; set; }
    public int? OpponentScore { get; set; }
    public int TotalCount { get; set; }

    /// <summary>true = entra al pool de emparejamiento aleatorio.</summary>
    public bool IsOpenToPublic { get; set; }

    public DuelStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
