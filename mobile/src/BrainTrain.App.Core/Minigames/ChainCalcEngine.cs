namespace BrainTrain.App.Core.Minigames;

public sealed record ChainRound(IReadOnlyList<string> Steps, int Answer, IReadOnlyList<int> Options);

/// <summary>
/// Cálculo en Cadena ("resuélvelo en 30 s"): la operación se revela por partes
/// (p. ej. "7", "+ 5", "× 2", "− 4") y cada paso se aplica sobre el resultado
/// anterior, SIN precedencia. 5 rondas con dificultad creciente (3 → 6 pasos,
/// números mayores) y nunca hay resultados intermedios negativos.
/// El VM pone los tiempos (1.2 s por paso, 30 s por ronda).
/// </summary>
public sealed class ChainCalcEngine(Random? rng = null)
{
    public const int TotalRounds = 5;

    private readonly Random _rng = rng ?? Random.Shared;
    private bool _answered = true;

    public int RoundNumber { get; private set; }
    public int Score { get; private set; }
    public ChainRound Current { get; private set; } = null!;
    public bool IsComplete => _answered && RoundNumber >= TotalRounds;

    public void Reset()
    {
        RoundNumber = 0;
        Score = 0;
        _answered = true;
        Current = null!;
    }

    /// <summary>Genera la siguiente ronda: pasos a revelar, respuesta y 4 opciones únicas.</summary>
    public ChainRound NextRound()
    {
        RoundNumber++;
        var stepCount = Math.Min(6, 2 + RoundNumber); // 3, 4, 5, 6, 6
        var maxAdd = 4 + RoundNumber * 3;             // números mayores por ronda

        var value = _rng.Next(2, maxAdd);
        var steps = new List<string> { value.ToString() };

        for (var i = 1; i < stepCount; i++)
        {
            var op = _rng.Next(3);
            switch (op)
            {
                case 1 when value >= 2: // resta acotada: sin negativos, apto para niños
                    var s = _rng.Next(1, Math.Min(value, maxAdd) + 1);
                    steps.Add($"− {s}");
                    value -= s;
                    break;

                case 2 when value is >= 2 and <= 30: // producto pequeño: ×2 o ×3
                    var m = _rng.Next(2, 4);
                    steps.Add($"× {m}");
                    value *= m;
                    break;

                default:
                    var a = _rng.Next(2, maxAdd);
                    steps.Add($"+ {a}");
                    value += a;
                    break;
            }
        }

        // 4 opciones: la correcta + 3 distractores plausibles, únicos y no negativos.
        var options = new HashSet<int> { value };
        while (options.Count < 4)
        {
            var delta = _rng.Next(1, 6 + RoundNumber * 2) * (_rng.Next(2) == 0 ? -1 : 1);
            var candidate = value + delta;
            if (candidate >= 0) options.Add(candidate);
        }

        _answered = false;
        Current = new ChainRound(steps, value, options.OrderBy(_ => _rng.Next()).ToList());
        return Current;
    }

    /// <summary>Responde la ronda actual; true si acertó. Usar -1 para "sin respuesta" (timeout).</summary>
    public bool Answer(int option)
    {
        if (_answered) return false;
        _answered = true;
        var ok = option == Current.Answer;
        if (ok) Score++;
        return ok;
    }
}
