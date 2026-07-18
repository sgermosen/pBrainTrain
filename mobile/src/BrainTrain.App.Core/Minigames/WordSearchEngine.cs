namespace BrainTrain.App.Core.Minigames;

/// <summary>
/// Sopa de Letras: genera la cuadrícula colocando palabras en 8 direcciones y
/// valida selecciones por línea recta (inicio → fin, en ambos sentidos).
/// </summary>
public sealed class WordSearchEngine(Random? rng = null)
{
    public const int Size = 10;
    private static readonly string[] Pool =
    [
        "GATO", "PERRO", "LIBRO", "CIELO", "VERDE", "FUEGO", "AGUA", "TIERRA",
        "NUBE", "FLOR", "PIEDRA", "VIENTO", "LUNA", "ESTRELLA", "PLAYA", "MONTE",
        "CASA", "PUENTE", "TREN", "BARCO", "QUESO", "LIMON", "MANGO", "UVA",
        "TIGRE", "LEON", "ZORRO", "DELFIN", "BALLENA", "ARDILLA",
        "CEREBRO", "LOGICA", "INGENIO", "MEMORIA", "NUMERO", "LETRA"
    ];
    private static readonly (int Dr, int Dc)[] Directions =
        [(0, 1), (1, 0), (1, 1), (1, -1), (0, -1), (-1, 0), (-1, -1), (-1, 1)];

    private readonly Random _rng = rng ?? Random.Shared;
    private readonly char[,] _grid = new char[Size, Size];
    private readonly List<string> _words = [];
    private readonly HashSet<string> _found = [];

    public IReadOnlyList<string> Words => _words;
    public IReadOnlyCollection<string> Found => _found;
    public int Score => _found.Count;
    public bool IsComplete => _found.Count == _words.Count;
    public char this[int row, int col] => _grid[row, col];

    public void Generate(int wordCount = 6)
    {
        _words.Clear();
        _found.Clear();
        for (var r = 0; r < Size; r++)
            for (var c = 0; c < Size; c++)
                _grid[r, c] = '\0';

        foreach (var word in Pool.OrderBy(_ => _rng.Next()))
        {
            if (_words.Count >= wordCount) break;
            if (TryPlace(word)) _words.Add(word);
        }

        // Relleno con letras frecuentes del español.
        const string filler = "AAEEIIOOUURRSSNNLLTTCDMBPGVFHQ";
        for (var r = 0; r < Size; r++)
            for (var c = 0; c < Size; c++)
                if (_grid[r, c] == '\0')
                    _grid[r, c] = filler[_rng.Next(filler.Length)];
    }

    private bool TryPlace(string word)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            var (dr, dc) = Directions[_rng.Next(Directions.Length)];
            var row = _rng.Next(Size);
            var col = _rng.Next(Size);
            var endR = row + dr * (word.Length - 1);
            var endC = col + dc * (word.Length - 1);
            if (endR is < 0 or >= Size || endC is < 0 or >= Size) continue;

            var fits = true;
            for (var i = 0; i < word.Length && fits; i++)
            {
                var ch = _grid[row + dr * i, col + dc * i];
                fits = ch == '\0' || ch == word[i]; // se permiten cruces compatibles
            }
            if (!fits) continue;

            for (var i = 0; i < word.Length; i++)
                _grid[row + dr * i, col + dc * i] = word[i];
            return true;
        }
        return false;
    }

    /// <summary>
    /// Valida la selección desde (r1,c1) hasta (r2,c2). Si es una palabra
    /// pendiente (derecha o al revés), la marca encontrada y devuelve
    /// las celdas que la componen.
    /// </summary>
    public IReadOnlyList<(int Row, int Col)>? TrySelect(int r1, int c1, int r2, int c2)
    {
        var dr = Math.Sign(r2 - r1);
        var dc = Math.Sign(c2 - c1);
        var len = Math.Max(Math.Abs(r2 - r1), Math.Abs(c2 - c1)) + 1;

        // Debe ser línea recta: horizontal, vertical o diagonal perfecta.
        if (dr != 0 && dc != 0 && Math.Abs(r2 - r1) != Math.Abs(c2 - c1)) return null;
        if (dr == 0 && dc == 0) return null;

        var cells = new List<(int, int)>(len);
        var chars = new char[len];
        for (var i = 0; i < len; i++)
        {
            var r = r1 + dr * i;
            var c = c1 + dc * i;
            if (r is < 0 or >= Size || c is < 0 or >= Size) return null;
            cells.Add((r, c));
            chars[i] = _grid[r, c];
        }

        var text = new string(chars);
        var reversed = new string(chars.Reverse().ToArray());
        var match = _words.FirstOrDefault(w => !_found.Contains(w) && (w == text || w == reversed));
        if (match is null) return null;

        _found.Add(match);
        return cells;
    }
}
