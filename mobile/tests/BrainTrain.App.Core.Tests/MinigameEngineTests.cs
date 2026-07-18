using BrainTrain.App.Core.Minigames;

namespace BrainTrain.App.Core.Tests;

public class Game2048EngineTests
{
    [Theory]
    [InlineData(new[] { 2, 2, 0, 0 }, new[] { 4, 0, 0, 0 }, 4)]
    [InlineData(new[] { 2, 2, 2, 2 }, new[] { 4, 4, 0, 0 }, 8)]
    [InlineData(new[] { 2, 0, 0, 2 }, new[] { 4, 0, 0, 0 }, 4)]
    [InlineData(new[] { 4, 2, 2, 0 }, new[] { 4, 4, 0, 0 }, 4)]
    [InlineData(new[] { 2, 4, 8, 16 }, new[] { 2, 4, 8, 16 }, 0)]
    [InlineData(new[] { 0, 0, 0, 2 }, new[] { 2, 0, 0, 0 }, 0)]
    public void MergeLine_SigueLasReglasClasicas(int[] input, int[] expected, int gained)
    {
        var (result, points, _) = Game2048Engine.MergeLine(input);
        Assert.Equal(expected, result);
        Assert.Equal(gained, points);
    }

    [Fact]
    public void Reset_EmpiezaConDosFichas()
    {
        var engine = new Game2048Engine(new Random(1));
        engine.Reset();
        Assert.Equal(2, engine.Cells.Count(v => v != 0));
        Assert.Equal(0, engine.Score);
    }

    [Fact]
    public void Move_AgregaFichaYSumaPuntaje()
    {
        var engine = new Game2048Engine(new Random(7));
        engine.Reset();
        var before = engine.Cells.Sum();

        // Con semilla fija, en pocos movimientos algo se mueve y aparece ficha nueva.
        var anyMove = new[] { MoveDirection.Left, MoveDirection.Up, MoveDirection.Right, MoveDirection.Down }
            .Any(engine.Move);
        Assert.True(anyMove);
        Assert.True(engine.Cells.Sum() > before);
    }

    [Fact]
    public void JuegaPartidaCompletaSinInconsistencias()
    {
        var rng = new Random(42);
        var engine = new Game2048Engine(rng);
        engine.Reset();

        for (var i = 0; i < 500 && !engine.IsGameOver(); i++)
        {
            var dir = (MoveDirection)rng.Next(4);
            engine.Move(dir);
            Assert.All(engine.Cells, v => Assert.True(v == 0 || (v & (v - 1)) == 0)); // potencias de 2
        }
        Assert.True(engine.Score >= 0);
    }
}

public class MathSprintEngineTests
{
    [Fact]
    public void LasOpcionesSiempreIncluyenLaRespuesta()
    {
        var engine = new MathSprintEngine(new Random(3));
        engine.Start();
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(4, engine.Current.Options.Count);
            Assert.Contains(engine.Current.Answer, engine.Current.Options);
            Assert.Equal(4, engine.Current.Options.Distinct().Count());
            engine.Answer(engine.Current.Options[0]);
        }
    }

    [Fact]
    public void CuentaAciertosYErrores()
    {
        var engine = new MathSprintEngine(new Random(5));
        engine.Start();

        Assert.True(engine.Answer(engine.Current.Answer));
        var wrong = engine.Current.Options.First(o => o != engine.Current.Answer);
        Assert.False(engine.Answer(wrong));

        Assert.Equal(1, engine.Correct);
        Assert.Equal(1, engine.Wrong);
    }

    [Fact]
    public void SinRestasNegativas()
    {
        var engine = new MathSprintEngine(new Random(11));
        engine.Start();
        for (var i = 0; i < 200; i++)
        {
            Assert.True(engine.Current.Answer >= 0, $"resultado negativo en {engine.Current.Text}");
            engine.Answer(engine.Current.Answer); // acierta para subir la dificultad
        }
    }
}

public class WordSearchEngineTests
{
    [Fact]
    public void Generate_ColocaLasPalabrasEnLaCuadricula()
    {
        var engine = new WordSearchEngine(new Random(9));
        engine.Generate(6);

        Assert.Equal(6, engine.Words.Count);
        Assert.Empty(engine.Found);

        // Toda celda tiene letra A-Z.
        for (var r = 0; r < WordSearchEngine.Size; r++)
            for (var c = 0; c < WordSearchEngine.Size; c++)
                Assert.InRange(engine[r, c], 'A', 'Z');
    }

    [Fact]
    public void TrySelect_EncuentraPalabrasEnAmbosSentidos()
    {
        var engine = new WordSearchEngine(new Random(9));
        engine.Generate(6);

        // Busca cada palabra por fuerza bruta igual que lo haría un jugador.
        foreach (var word in engine.Words.ToList())
        {
            var found = FindWord(engine, word);
            Assert.NotNull(found);
            var cells = engine.TrySelect(found.Value.r1, found.Value.c1, found.Value.r2, found.Value.c2);
            Assert.NotNull(cells);
            Assert.Equal(word.Length, cells!.Count);
        }
        Assert.True(engine.IsComplete);
        Assert.Equal(engine.Words.Count, engine.Score);
    }

    [Fact]
    public void TrySelect_RechazaSeleccionesInvalidas()
    {
        var engine = new WordSearchEngine(new Random(4));
        engine.Generate(6);

        Assert.Null(engine.TrySelect(0, 0, 0, 0));      // misma celda
        Assert.Null(engine.TrySelect(0, 0, 2, 5));      // no es línea recta
        Assert.Null(engine.TrySelect(0, 0, -1, 0));     // fuera de rango
    }

    private static (int r1, int c1, int r2, int c2)? FindWord(WordSearchEngine e, string word)
    {
        (int, int)[] dirs = [(0, 1), (1, 0), (1, 1), (1, -1), (0, -1), (-1, 0), (-1, -1), (-1, 1)];
        for (var r = 0; r < WordSearchEngine.Size; r++)
            for (var c = 0; c < WordSearchEngine.Size; c++)
                foreach (var (dr, dc) in dirs)
                {
                    var endR = r + dr * (word.Length - 1);
                    var endC = c + dc * (word.Length - 1);
                    if (endR is < 0 or >= WordSearchEngine.Size || endC is < 0 or >= WordSearchEngine.Size) continue;
                    var ok = true;
                    for (var i = 0; i < word.Length && ok; i++)
                        ok = e[r + dr * i, c + dc * i] == word[i];
                    if (ok) return (r, c, endR, endC);
                }
        return null;
    }
}
