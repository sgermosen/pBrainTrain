using BrainTrain.Api;
using BrainTrain.Api.Services;
using BrainTrain.Domain;

namespace BrainTrain.Tests;

public class GameLogicTests
{
    private static CatalogSnapshot Snapshot(int questions)
    {
        var byId = new Dictionary<int, CatalogQuestion>();
        for (var i = 1; i <= questions; i++)
        {
            byId[i] = new CatalogQuestion(i, CategoryId: 1 + i % 3, QuestionType.MultipleChoice,
                Difficulty: 1 + i % 5, $"P{i}", "expl", null,
                [new CatalogChoice(i * 10, "a", true), new CatalogChoice(i * 10 + 1, "b", false)]);
        }
        var byCat = byId.Values.GroupBy(q => q.CategoryId).ToDictionary(g => g.Key, g => g.Select(q => q.Id).ToArray());
        return new CatalogSnapshot([], byId, byCat, byId.Keys.ToArray());
    }

    [Fact]
    public void PickQuestions_DevuelveCantidadExactaSinRepetidos()
    {
        var snap = Snapshot(60);
        var picked = GameService.PickQuestions(snap, snap.AllQuestionIds, 7, 2.5, new Random(42));
        Assert.Equal(7, picked.Length);
        Assert.Equal(7, picked.Distinct().Count());
    }

    [Fact]
    public void PickQuestions_ConPoolChicoDevuelveTodo()
    {
        var snap = Snapshot(4);
        var picked = GameService.PickQuestions(snap, snap.AllQuestionIds, 7, 3, new Random(1));
        Assert.Equal(4, picked.Length);
    }

    [Fact]
    public void PickQuestions_SesgaHaciaLaDificultadObjetivo()
    {
        var snap = Snapshot(200);
        var rng = new Random(7);
        var easy = new List<int>();
        var hard = new List<int>();
        for (var i = 0; i < 200; i++)
        {
            easy.AddRange(GameService.PickQuestions(snap, snap.AllQuestionIds, 7, 1.0, rng)
                .Select(id => snap.QuestionsById[id].Difficulty));
            hard.AddRange(GameService.PickQuestions(snap, snap.AllQuestionIds, 7, 5.0, rng)
                .Select(id => snap.QuestionsById[id].Difficulty));
        }
        Assert.True(easy.Average() < hard.Average(),
            $"promedio fácil {easy.Average():F2} debería ser menor que difícil {hard.Average():F2}");
    }

    [Fact]
    public void DailyQuestionIds_EsDeterministaPorFecha()
    {
        var snap = Snapshot(50);
        var date = new DateOnly(2026, 7, 6);
        var a = GameService.DailyQuestionIds(snap, date, 7);
        var b = GameService.DailyQuestionIds(snap, date, 7);
        Assert.Equal(a, b);
        Assert.Equal(7, a.Distinct().Count());

        var otherDay = GameService.DailyQuestionIds(snap, date.AddDays(1), 7);
        Assert.NotEqual(a, otherDay);
    }

    [Fact]
    public void ParseIds_RoundTrip()
    {
        int[] ids = [5, 1, 99];
        Assert.Equal(ids, GameService.ParseIds(string.Join(',', ids)));
        Assert.Empty(GameService.ParseIds(""));
    }
}
