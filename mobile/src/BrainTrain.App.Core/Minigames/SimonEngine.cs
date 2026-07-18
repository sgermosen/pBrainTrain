namespace BrainTrain.App.Core.Minigames;

/// <summary>
/// Simón Dice: la secuencia de 4 colores crece un paso por ronda. El puntaje
/// son las rondas completadas (longitud de la secuencia repetida sin error).
/// </summary>
public sealed class SimonEngine(Random? rng = null)
{
    private readonly Random _rng = rng ?? Random.Shared;
    private readonly List<int> _sequence = [];
    private int _inputPos;

    public IReadOnlyList<int> Sequence => _sequence;
    public int CompletedRounds { get; private set; }
    public bool IsGameOver { get; private set; }

    public void Reset()
    {
        _sequence.Clear();
        CompletedRounds = 0;
        IsGameOver = false;
        Extend();
    }

    /// <summary>Agrega un color a la secuencia y reinicia la entrada del jugador.</summary>
    public void Extend()
    {
        _sequence.Add(_rng.Next(4));
        _inputPos = 0;
    }

    /// <summary>Devuelve true si la entrada es correcta; false = juego terminado.
    /// RoundComplete indica que hay que reproducir la siguiente ronda.</summary>
    public bool Input(int color, out bool roundComplete)
    {
        roundComplete = false;
        if (IsGameOver) return false;

        if (color != _sequence[_inputPos])
        {
            IsGameOver = true;
            return false;
        }

        _inputPos++;
        if (_inputPos == _sequence.Count)
        {
            CompletedRounds++;
            roundComplete = true;
        }
        return true;
    }
}
