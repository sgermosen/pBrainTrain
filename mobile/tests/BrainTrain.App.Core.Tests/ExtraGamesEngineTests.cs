using BrainTrain.App.Core.Minigames;

namespace BrainTrain.App.Core.Tests;

public class MemoryPairsEngineTests
{
    [Fact]
    public void Reset_Genera16CartasCon8Parejas()
    {
        var engine = new MemoryPairsEngine(new Random(3));
        engine.Reset();
        Assert.Equal(16, engine.Faces.Count);
        Assert.All(engine.Faces.GroupBy(f => f), g => Assert.Equal(2, g.Count()));
    }

    [Fact]
    public void FlujoCompleto_EncuentraTodasLasParejas()
    {
        var engine = new MemoryPairsEngine(new Random(5));
        engine.Reset();

        // Juega con "memoria perfecta": busca cada pareja por su cara.
        foreach (var group in engine.Faces
                     .Select((face, i) => (face, i))
                     .GroupBy(x => x.face))
        {
            var idx = group.Select(x => x.i).ToArray();
            Assert.Equal(FlipResult.First, engine.Flip(idx[0]));
            Assert.Equal(FlipResult.Matched, engine.Flip(idx[1]));
        }
        Assert.True(engine.IsComplete);
        Assert.Equal(8, engine.MatchedPairs);
        Assert.Equal(8, engine.Moves);
    }

    [Fact]
    public void Fallo_QuedaPendienteHastaLimpiar()
    {
        var engine = new MemoryPairsEngine(new Random(1));
        engine.Reset();
        var first = 0;
        var mismatch = Enumerable.Range(1, 15).First(i => engine.Faces[i] != engine.Faces[first]);

        engine.Flip(first);
        Assert.Equal(FlipResult.Mismatch, engine.Flip(mismatch));
        // Mientras el fallo está visible, no se aceptan más volteos.
        Assert.Equal(FlipResult.Ignored, engine.Flip(2));
        var pending = engine.ClearPending();
        Assert.NotNull(pending);
        Assert.Equal(FlipResult.First, engine.Flip(2));
    }

    [Fact]
    public void NoPermiteVoltearLaMismaCartaDosVeces()
    {
        var engine = new MemoryPairsEngine(new Random(2));
        engine.Reset();
        engine.Flip(4);
        Assert.Equal(FlipResult.Ignored, engine.Flip(4));
    }
}

public class SimonEngineTests
{
    [Fact]
    public void SecuenciaCreceYSeRepiteCorrectamente()
    {
        var engine = new SimonEngine(new Random(7));
        engine.Reset();
        Assert.Single(engine.Sequence);

        // Repite 10 rondas perfectas.
        for (var round = 1; round <= 10; round++)
        {
            var roundComplete = false;
            foreach (var color in engine.Sequence.ToArray())
                Assert.True(engine.Input(color, out roundComplete));
            Assert.True(roundComplete);
            Assert.Equal(round, engine.CompletedRounds);
            engine.Extend();
            Assert.Equal(round + 1, engine.Sequence.Count);
        }
    }

    [Fact]
    public void ErrorTerminaElJuego()
    {
        var engine = new SimonEngine(new Random(9));
        engine.Reset();
        var wrong = (engine.Sequence[0] + 1) % 4;
        Assert.False(engine.Input(wrong, out _));
        Assert.True(engine.IsGameOver);
        Assert.Equal(0, engine.CompletedRounds);
        Assert.False(engine.Input(engine.Sequence[0], out _)); // ya no acepta entradas
    }
}

public class SpotDiffEngineTests
{
    [Fact]
    public void EscenasGeneradas_TresCon5DiferenciasCadaUna()
    {
        Assert.Equal(3, SpotDiffScenes.All.Count);
        Assert.All(SpotDiffScenes.All, s =>
        {
            Assert.Equal(5, s.Diffs.Count);
            Assert.All(s.Diffs, d =>
            {
                Assert.InRange(d.X, 0, 1);
                Assert.InRange(d.Y, 0, 1);
            });
            Assert.EndsWith(".png", s.ImageA);
            Assert.EndsWith(".png", s.ImageB);
        });
    }

    [Fact]
    public void TocarLaDiferencia_LaEncuentra_YNoSeRepite()
    {
        var engine = new SpotDiffEngine();
        engine.Reset();
        var d = engine.Current.Diffs[0];

        Assert.Equal(0, engine.TryTap(d.X, d.Y));
        Assert.Null(engine.TryTap(d.X, d.Y)); // ya encontrada
        Assert.Equal(1, engine.Score);
    }

    [Fact]
    public void TocarLejos_NoCuenta()
    {
        var engine = new SpotDiffEngine();
        engine.Reset();
        // Esquina donde ninguna escena tiene diferencia a <2 radios.
        var far = engine.Current.Diffs.All(d =>
            Math.Abs(d.X - 0.98) > SpotDiffEngine.HitRadiusX * 2 ||
            Math.Abs(d.Y - 0.02) > SpotDiffEngine.HitRadiusY * 2);
        if (far)
            Assert.Null(engine.TryTap(0.98, 0.02));
    }

    [Fact]
    public void FlujoCompleto_TresEscenas15Diferencias()
    {
        var engine = new SpotDiffEngine();
        engine.Reset();
        for (var s = 0; s < engine.SceneCount; s++)
        {
            foreach (var d in engine.Current.Diffs)
                Assert.NotNull(engine.TryTap(d.X, d.Y));
            Assert.True(engine.SceneComplete);
            if (!engine.IsLastScene)
                Assert.True(engine.NextScene());
        }
        Assert.Equal(15, engine.Score);
        Assert.False(engine.NextScene());
    }
}

public class RubikGuideTests
{
    [Fact]
    public void LaGuiaTiene9PasosCompletos()
    {
        Assert.Equal(9, RubikGuide.Steps.Count);
        Assert.All(RubikGuide.Steps, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Title));
            Assert.False(string.IsNullOrWhiteSpace(s.Goal));
            Assert.False(string.IsNullOrWhiteSpace(s.Tip));
            Assert.NotEmpty(s.Algorithms);
            if (s.FacePattern is not null)
            {
                Assert.Equal(9, s.FacePattern.Length);
                Assert.All(s.FacePattern, c => Assert.Contains(c, "WYRGBOx"));
            }
        });
    }

    [Fact]
    public void TerminaConElCuboArmado()
    {
        Assert.Equal("YYYYYYYYY", RubikGuide.Steps[^1].FacePattern);
    }
}
