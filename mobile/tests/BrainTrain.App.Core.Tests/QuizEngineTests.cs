using BrainTrain.App.Core;

namespace BrainTrain.App.Core.Tests;

public class QuizEngineTests
{
    private static List<QuestionDto> Questions(int n) =>
        Enumerable.Range(1, n).Select(i => new QuestionDto(
            i, 1, "multipleChoice", 2, $"Pregunta {i}",
            [new ChoiceDto(i * 10, "A"), new ChoiceDto(i * 10 + 1, "B")])).ToList();

    [Fact]
    public void RecorreTodasLasPreguntasYConstruyeElEnvio()
    {
        var engine = new QuizEngine(Questions(3));
        Assert.Equal(3, engine.Total);
        Assert.False(engine.IsFinished);

        engine.Answer(10);   // correcta o no, da igual: el cliente no lo sabe
        engine.Answer(null); // tiempo agotado
        engine.Answer(31);

        Assert.True(engine.IsFinished);
        var submission = engine.BuildSubmission();
        Assert.Equal(3, submission.Count);
        Assert.Equal(10, submission[0].ChoiceId);
        Assert.Null(submission[1].ChoiceId);
        Assert.Equal(2, engine.AnsweredCount);
    }

    [Fact]
    public void NoPermiteResponderTrasTerminar()
    {
        var engine = new QuizEngine(Questions(1));
        engine.Answer(10);
        Assert.Throws<InvalidOperationException>(() => engine.Answer(11));
    }

    [Fact]
    public void ElProgresoAvanzaDeCeroAUno()
    {
        var engine = new QuizEngine(Questions(4));
        Assert.Equal(0, engine.Progress);
        engine.Answer(null);
        engine.Answer(null);
        Assert.Equal(0.5, engine.Progress);
    }
}
