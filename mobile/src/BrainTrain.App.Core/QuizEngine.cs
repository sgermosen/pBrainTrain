namespace BrainTrain.App.Core;

/// <summary>
/// Estado puro de una partida en el cliente: pregunta actual, respuestas
/// elegidas y construcción del envío final. Sin dependencias de UI ni de red,
/// para poder probarlo de forma exhaustiva.
/// </summary>
public sealed class QuizEngine(IReadOnlyList<QuestionDto> questions, int secondsPerQuestion = 25)
{
    private readonly List<SubmitAnswer> _answers = [];

    public int SecondsPerQuestion { get; } = secondsPerQuestion;
    public int Index { get; private set; }
    public int Total => questions.Count;
    public bool IsFinished => Index >= questions.Count;
    public QuestionDto Current => questions[Index];
    public int AnsweredCount => _answers.Count(a => a.ChoiceId is not null);
    public double Progress => Total == 0 ? 1 : (double)Index / Total;

    /// <summary>Registra la elección (o null si se agotó el tiempo) y avanza.</summary>
    public void Answer(int? choiceId)
    {
        if (IsFinished)
            throw new InvalidOperationException("La partida ya terminó.");
        _answers.Add(new SubmitAnswer(Current.Id, choiceId));
        Index++;
    }

    public List<SubmitAnswer> BuildSubmission() => [.. _answers];
}
