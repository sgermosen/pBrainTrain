namespace BrainTrain.App.Core.Minigames;

public enum FlipResult { Ignored, First, Matched, Mismatch }

/// <summary>
/// Parejas de memoria clásico: 16 cartas (8 parejas de emojis). El motor lleva
/// el estado; la UI decide cuándo ocultar los fallos (ClearPending).
/// </summary>
public sealed class MemoryPairsEngine(Random? rng = null)
{
    private static readonly string[] Pool =
        ["🐶", "🐱", "🦊", "🐼", "🦁", "🐸", "🦉", "🐧", "🐢", "🦄", "🐙", "🦜"];

    private readonly Random _rng = rng ?? Random.Shared;
    private readonly List<string> _faces = [];
    private readonly bool[] _matched = new bool[16];
    private int? _first;
    private (int A, int B)? _pendingMismatch;

    public IReadOnlyList<string> Faces => _faces;
    public int Moves { get; private set; }
    public int MatchedPairs { get; private set; }
    public bool IsComplete => MatchedPairs == 8;

    public void Reset()
    {
        _faces.Clear();
        var chosen = Pool.OrderBy(_ => _rng.Next()).Take(8).ToArray();
        _faces.AddRange(chosen.Concat(chosen).OrderBy(_ => _rng.Next()));
        Array.Clear(_matched);
        _first = null;
        _pendingMismatch = null;
        Moves = 0;
        MatchedPairs = 0;
    }

    public bool IsMatched(int index) => _matched[index];
    public (int A, int B)? PendingMismatch => _pendingMismatch;

    public FlipResult Flip(int index)
    {
        if (index < 0 || index >= 16 || _matched[index] || _pendingMismatch is not null || _first == index)
            return FlipResult.Ignored;

        if (_first is null)
        {
            _first = index;
            return FlipResult.First;
        }

        Moves++;
        var first = _first.Value;
        _first = null;
        if (_faces[first] == _faces[index])
        {
            _matched[first] = _matched[index] = true;
            MatchedPairs++;
            return FlipResult.Matched;
        }

        _pendingMismatch = (first, index);
        return FlipResult.Mismatch;
    }

    /// <summary>La UI llama esto tras mostrar el fallo un instante.</summary>
    public (int A, int B)? ClearPending()
    {
        var p = _pendingMismatch;
        _pendingMismatch = null;
        return p;
    }
}
