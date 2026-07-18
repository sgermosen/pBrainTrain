namespace BrainTrain.App.Core.Minigames;

public sealed record MathProblem(string Text, int Answer, IReadOnlyList<int> Options);

/// <summary>
/// Cálculo Rápido (estilo Math Master): operaciones que suben de dificultad
/// con cada acierto. El VM pone el reloj (60 s); el motor solo genera y corrige.
/// </summary>
public sealed class MathSprintEngine(Random? rng = null)
{
    private readonly Random _rng = rng ?? Random.Shared;

    public int Correct { get; private set; }
    public int Wrong { get; private set; }
    public MathProblem Current { get; private set; } = null!;

    public void Start()
    {
        Correct = 0;
        Wrong = 0;
        Next();
    }

    /// <summary>Responde la operación actual; true si fue correcta. Genera la siguiente.</summary>
    public bool Answer(int option)
    {
        var ok = option == Current.Answer;
        if (ok) Correct++; else Wrong++;
        Next();
        return ok;
    }

    private void Next()
    {
        // Dificultad progresiva: los rangos crecen con los aciertos.
        var tier = Math.Min(4, Correct / 5);
        var (text, answer) = _rng.Next(tier >= 2 ? 3 : 2) switch
        {
            0 => Add(tier),
            1 => Subtract(tier),
            _ => Multiply(tier)
        };

        // 4 opciones: la correcta + 3 distractores plausibles y únicos.
        var options = new HashSet<int> { answer };
        while (options.Count < 4)
        {
            var delta = _rng.Next(1, 10 + tier * 5) * (_rng.Next(2) == 0 ? -1 : 1);
            options.Add(answer + delta);
        }
        Current = new MathProblem(text, answer, options.OrderBy(_ => _rng.Next()).ToList());
    }

    private (string, int) Add(int tier)
    {
        int max = 20 + tier * 40;
        int a = _rng.Next(2, max), b = _rng.Next(2, max);
        return ($"{a} + {b}", a + b);
    }

    private (string, int) Subtract(int tier)
    {
        int max = 20 + tier * 40;
        int a = _rng.Next(2, max), b = _rng.Next(2, max);
        if (b > a) (a, b) = (b, a); // sin negativos: apto para niños
        return ($"{a} − {b}", a - b);
    }

    private (string, int) Multiply(int tier)
    {
        int max = 6 + tier * 3;
        int a = _rng.Next(2, max), b = _rng.Next(2, 10);
        return ($"{a} × {b}", a * b);
    }
}
