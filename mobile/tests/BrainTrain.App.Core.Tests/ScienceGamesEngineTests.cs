using BrainTrain.App.Core.Minigames;

namespace BrainTrain.App.Core.Tests;

public class StroopEngineTests
{
    [Fact]
    public void Start_GeneraEnsayoValido()
    {
        var engine = new StroopEngine(new Random(3));
        engine.Start();

        Assert.Equal(0, engine.Correct);
        Assert.Equal(0, engine.Wrong);
        Assert.Contains(engine.Current.Word, StroopEngine.Words);
        Assert.InRange(engine.Current.WordColorIndex, 0, 3);
    }

    [Fact]
    public void AlrededorDel60PorCientoEsIncongruente()
    {
        var engine = new StroopEngine(new Random(7));
        engine.Start();

        const int trials = 1000;
        var incongruent = 0;
        for (var i = 0; i < trials; i++)
        {
            var wordIndex = StroopEngine.Words.ToList().IndexOf(engine.Current.Word);
            if (wordIndex != engine.Current.WordColorIndex) incongruent++;
            engine.Answer(engine.Current.WordColorIndex);
        }
        Assert.InRange(incongruent / (double)trials, 0.5, 0.7);
    }

    [Fact]
    public void Answer_CuentaAciertosYErrores()
    {
        var engine = new StroopEngine(new Random(5));
        engine.Start();

        Assert.True(engine.Answer(engine.Current.WordColorIndex));   // tinta correcta
        Assert.False(engine.Answer((engine.Current.WordColorIndex + 1) % 4)); // tinta incorrecta

        Assert.Equal(1, engine.Correct);
        Assert.Equal(1, engine.Wrong);
    }

    [Fact]
    public void Answer_SiempreGeneraUnEnsayoValidoNuevo()
    {
        var engine = new StroopEngine(new Random(11));
        engine.Start();
        for (var i = 0; i < 100; i++)
        {
            engine.Answer(engine.Current.WordColorIndex);
            Assert.Contains(engine.Current.Word, StroopEngine.Words);
            Assert.InRange(engine.Current.WordColorIndex, 0, 3);
        }
        Assert.Equal(100, engine.Correct);
    }
}

public class ChainCalcEngineTests
{
    [Fact]
    public void NextRound_OpcionesUnicasConLaRespuestaIncluida()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var engine = new ChainCalcEngine(new Random(seed));
            engine.Reset();
            for (var r = 0; r < ChainCalcEngine.TotalRounds; r++)
            {
                var round = engine.NextRound();
                Assert.Equal(4, round.Options.Count);
                Assert.Equal(4, round.Options.Distinct().Count());
                Assert.Contains(round.Answer, round.Options);
                Assert.All(round.Options, o => Assert.True(o >= 0));
                engine.Answer(round.Answer);
            }
        }
    }

    [Fact]
    public void CadenaSinNegativosIntermedios_YRespuestaConsistente()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var engine = new ChainCalcEngine(new Random(seed));
            engine.Reset();
            for (var r = 0; r < ChainCalcEngine.TotalRounds; r++)
            {
                var round = engine.NextRound();
                var values = Recompute(round);
                Assert.All(values, v => Assert.True(v >= 0, $"intermedio negativo en: {string.Join(" ", round.Steps)}"));
                Assert.Equal(round.Answer, values[^1]);
                engine.Answer(round.Answer);
            }
        }
    }

    [Fact]
    public void DificultadCrece_De3A6Pasos()
    {
        var engine = new ChainCalcEngine(new Random(9));
        engine.Reset();
        int[] expected = [3, 4, 5, 6, 6];
        foreach (var steps in expected)
        {
            var round = engine.NextRound();
            Assert.Equal(steps, round.Steps.Count);
            engine.Answer(round.Answer);
        }
    }

    [Fact]
    public void FlujoCompleto_CuentaLasRondasCorrectas()
    {
        var engine = new ChainCalcEngine(new Random(4));
        engine.Reset();

        for (var r = 1; r <= ChainCalcEngine.TotalRounds; r++)
        {
            var round = engine.NextRound();
            Assert.False(engine.IsComplete);
            if (r % 2 == 1)
                Assert.True(engine.Answer(round.Answer));
            else
                Assert.False(engine.Answer(-1)); // timeout: sin respuesta
        }
        Assert.True(engine.IsComplete);
        Assert.Equal(3, engine.Score); // rondas 1, 3 y 5
    }

    [Fact]
    public void Answer_DobleRespuestaSeIgnora()
    {
        var engine = new ChainCalcEngine(new Random(2));
        engine.Reset();
        var round = engine.NextRound();

        Assert.True(engine.Answer(round.Answer));
        Assert.False(engine.Answer(round.Answer)); // la ronda ya se respondió
        Assert.Equal(1, engine.Score);
    }

    /// <summary>Reaplica los pasos como cadena mental (sin precedencia) y devuelve cada valor intermedio.</summary>
    private static List<int> Recompute(ChainRound round)
    {
        var values = new List<int> { int.Parse(round.Steps[0]) };
        foreach (var step in round.Steps.Skip(1))
        {
            var parts = step.Split(' ');
            var n = int.Parse(parts[1]);
            values.Add(parts[0] switch
            {
                "+" => values[^1] + n,
                "−" => values[^1] - n,
                "×" => values[^1] * n,
                _ => throw new InvalidOperationException($"operador desconocido: {parts[0]}")
            });
        }
        return values;
    }
}

public class NBackEngineTests
{
    [Fact]
    public void Start_YAdvance_GeneranPosicionesValidas()
    {
        var engine = new NBackEngine(new Random(3));
        engine.Start();

        Assert.Equal(1, engine.N);
        Assert.Equal(0, engine.Responses);
        for (var i = 0; i < NBackEngine.TotalStimuli; i++)
        {
            var pos = engine.Advance();
            Assert.InRange(pos, 0, NBackEngine.GridCells - 1);
            Assert.Equal(pos, engine.CurrentPosition);
        }
    }

    [Fact]
    public void JuegoPerfecto_20AciertosYTransicionA2Back()
    {
        var engine = new NBackEngine(new Random(7));
        engine.Start();

        for (var i = 0; i < NBackEngine.TotalStimuli; i++)
        {
            engine.Advance();
            Assert.True(engine.RegisterResponse(engine.IsMatch)); // juego perfecto
            if (engine.Correct >= NBackEngine.PromoteAfterCorrect)
                Assert.Equal(2, engine.N); // tras 6 aciertos sube a 2-back
        }
        Assert.True(engine.IsComplete);
        Assert.Equal(NBackEngine.TotalStimuli, engine.Correct);
    }

    [Fact]
    public void RespuestaIncorrecta_NoSumaPeroCuentaElEstimulo()
    {
        var engine = new NBackEngine(new Random(9));
        engine.Start();
        engine.Advance();

        // El primer estímulo nunca puede ser match: presionar es error.
        Assert.False(engine.IsMatch);
        Assert.False(engine.RegisterResponse(true));
        Assert.Equal(0, engine.Correct);
        Assert.Equal(1, engine.Responses);
        Assert.Equal(1, engine.N);
    }

    [Fact]
    public void MatchForzado_ProduceProporcionCercanaAl30PorCiento()
    {
        var engine = new NBackEngine(new Random(13));
        engine.Start();

        // Sin registrar respuestas N se mantiene en 1; solo medimos la generación.
        const int stimuli = 3000;
        var matches = 0;
        for (var i = 0; i < stimuli; i++)
        {
            engine.Advance();
            if (engine.IsMatch) matches++;
        }
        // 30 % forzado + coincidencias casuales (~1/9 del 70 %) ≈ 0.38.
        Assert.InRange(matches / (double)stimuli, 0.30, 0.46);
    }
}
