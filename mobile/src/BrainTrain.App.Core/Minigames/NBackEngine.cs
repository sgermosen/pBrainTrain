namespace BrainTrain.App.Core.Minigames;

/// <summary>
/// N-Back visual simplificado en cuadrícula 3×3: cada estímulo ilumina una
/// celda y el jugador indica si la posición es igual a la de N pasos atrás.
/// Empieza en 1-back y tras 6 aciertos sube a 2-back. 20 estímulos por sesión;
/// el puntaje son las respuestas correctas. El VM pone el ritmo (2 s).
/// </summary>
public sealed class NBackEngine(Random? rng = null)
{
    public const int TotalStimuli = 20;
    public const int GridCells = 9;
    public const int PromoteAfterCorrect = 6;

    private readonly Random _rng = rng ?? Random.Shared;
    private readonly List<int> _history = [];

    public int N { get; private set; } = 1;
    public int Correct { get; private set; }
    public int Responses { get; private set; }
    public bool IsComplete => Responses >= TotalStimuli;
    public int CurrentPosition => _history[^1];

    /// <summary>La posición actual coincide con la de N pasos atrás.</summary>
    public bool IsMatch => _history.Count > N && _history[^1] == _history[^(N + 1)];

    public void Start()
    {
        _history.Clear();
        N = 1;
        Correct = 0;
        Responses = 0;
    }

    /// <summary>Genera el siguiente estímulo: 30 % de match forzado cuando ya hay historia.</summary>
    public int Advance()
    {
        var next = _history.Count >= N && _rng.Next(100) < 30
            ? _history[^N] // repite la posición de hace N pasos
            : _rng.Next(GridCells);
        _history.Add(next);
        return next;
    }

    /// <summary>Registra la respuesta del estímulo actual: correcta si pressed == IsMatch.
    /// Tras 6 aciertos en 1-back el nivel sube a 2-back.</summary>
    public bool RegisterResponse(bool pressed)
    {
        var ok = pressed == IsMatch;
        if (ok) Correct++;
        Responses++;
        if (N == 1 && Correct >= PromoteAfterCorrect) N = 2;
        return ok;
    }
}
