namespace BrainTrain.App.Core.Minigames;

public enum MoveDirection { Up, Down, Left, Right }

/// <summary>
/// Motor puro del 2048 clásico: tablero 4×4, fusión de fichas iguales y
/// puntaje por fusiones. Sin UI ni aleatoriedad oculta (RNG inyectable).
/// </summary>
public sealed class Game2048Engine(Random? rng = null)
{
    public const int Size = 4;
    private readonly Random _rng = rng ?? Random.Shared;
    private readonly int[] _cells = new int[Size * Size];

    public int Score { get; private set; }
    public int BestTile => _cells.Max();
    public IReadOnlyList<int> Cells => _cells;

    public void Reset()
    {
        Array.Clear(_cells);
        Score = 0;
        SpawnTile();
        SpawnTile();
    }

    public int this[int row, int col] => _cells[row * Size + col];

    /// <summary>Aplica el movimiento; devuelve true si algo cambió (y aparece ficha nueva).</summary>
    public bool Move(MoveDirection dir)
    {
        var moved = false;
        for (var line = 0; line < Size; line++)
        {
            var idx = LineIndices(dir, line);
            var values = idx.Select(i => _cells[i]).ToArray();
            var (merged, gained, changed) = MergeLine(values);
            if (!changed) continue;

            moved = true;
            Score += gained;
            for (var k = 0; k < Size; k++)
                _cells[idx[k]] = merged[k];
        }

        if (moved) SpawnTile();
        return moved;
    }

    public bool IsGameOver()
    {
        if (_cells.Contains(0)) return false;
        for (var r = 0; r < Size; r++)
            for (var c = 0; c < Size; c++)
            {
                var v = this[r, c];
                if (r + 1 < Size && this[r + 1, c] == v) return false;
                if (c + 1 < Size && this[r, c + 1] == v) return false;
            }
        return true;
    }

    /// <summary>Compacta y fusiona una línea hacia el inicio (regla clásica: una fusión por par).</summary>
    internal static (int[] Result, int Gained, bool Changed) MergeLine(int[] line)
    {
        var compact = line.Where(v => v != 0).ToList();
        var result = new List<int>(line.Length);
        var gained = 0;

        for (var i = 0; i < compact.Count; i++)
        {
            if (i + 1 < compact.Count && compact[i] == compact[i + 1])
            {
                var merged = compact[i] * 2;
                result.Add(merged);
                gained += merged;
                i++; // consume la pareja
            }
            else
            {
                result.Add(compact[i]);
            }
        }
        while (result.Count < line.Length) result.Add(0);

        var arr = result.ToArray();
        return (arr, gained, !arr.SequenceEqual(line));
    }

    private int[] LineIndices(MoveDirection dir, int line) => dir switch
    {
        // Índices en el orden en que la línea "cae" hacia el borde del movimiento.
        MoveDirection.Left => [line * Size, line * Size + 1, line * Size + 2, line * Size + 3],
        MoveDirection.Right => [line * Size + 3, line * Size + 2, line * Size + 1, line * Size],
        MoveDirection.Up => [line, line + Size, line + 2 * Size, line + 3 * Size],
        _ => [line + 3 * Size, line + 2 * Size, line + Size, line]
    };

    private void SpawnTile()
    {
        var empty = Enumerable.Range(0, _cells.Length).Where(i => _cells[i] == 0).ToArray();
        if (empty.Length == 0) return;
        _cells[empty[_rng.Next(empty.Length)]] = _rng.NextDouble() < 0.9 ? 2 : 4;
    }
}
