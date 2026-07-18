namespace BrainTrain.App.Core.Minigames;

public sealed record StroopTrial(string Word, int WordColorIndex);

/// <summary>
/// Test de Stroop: se muestra una palabra de color pintada con una tinta que
/// puede no coincidir (60 % incongruente). El jugador debe tocar el color de
/// la TINTA, no la palabra. El VM pone el reloj (60 s); el motor solo genera
/// y corrige. Los colores son índices 0-3: rojo, verde, azul, amarillo.
/// </summary>
public sealed class StroopEngine(Random? rng = null)
{
    public static readonly IReadOnlyList<string> Words = ["ROJO", "VERDE", "AZUL", "AMARILLO"];

    private readonly Random _rng = rng ?? Random.Shared;

    public int Correct { get; private set; }
    public int Wrong { get; private set; }
    public StroopTrial Current { get; private set; } = null!;

    public void Start()
    {
        Correct = 0;
        Wrong = 0;
        Next();
    }

    /// <summary>Responde con el índice del color de la tinta; true si acertó. Genera el siguiente.</summary>
    public bool Answer(int colorIndex)
    {
        var ok = colorIndex == Current.WordColorIndex;
        if (ok) Correct++; else Wrong++;
        Next();
        return ok;
    }

    private void Next()
    {
        var word = _rng.Next(Words.Count);
        int ink;
        if (_rng.Next(100) < 60)
        {
            // Incongruente: la tinta nunca coincide con la palabra.
            ink = _rng.Next(Words.Count - 1);
            if (ink >= word) ink++;
        }
        else
        {
            ink = word; // congruente
        }
        Current = new StroopTrial(Words[word], ink);
    }
}
