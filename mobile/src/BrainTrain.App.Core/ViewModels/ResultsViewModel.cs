using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

public sealed record ReviewItem(
    string Question, string CorrectAnswer, string? YourAnswer, bool WasCorrect,
    string Explanation, string? FunFact)
{
    public string Icon => WasCorrect ? "✅" : "💡";
}

/// <summary>
/// Cierre de partida: celebración proporcional al logro (refuerzo positivo,
/// no dopamina vacía) y repaso didáctico con la lógica de cada respuesta —
/// el momento de aprendizaje más importante de la app.
/// </summary>
public partial class ResultsViewModel(GameFlow flow, INavigationService nav, IShareService? share = null) : ObservableObject
{
    [ObservableProperty] private string _headline = "";
    [ObservableProperty] private string _scoreLabel = "";
    [ObservableProperty] private string _xpLabel = "";
    [ObservableProperty] private string _coinsLabel = "";
    [ObservableProperty] private string _streakLabel = "";
    [ObservableProperty] private bool _isPerfect;
    [ObservableProperty] private bool _leveledUp;
    [ObservableProperty] private int _newLevel;

    public ObservableCollection<UnlockedAchievementDto> Unlocked { get; } = [];
    public ObservableCollection<ReviewItem> Review { get; } = [];

    public void Load()
    {
        var r = flow.LastResult;
        var game = flow.CurrentGame;
        if (r is null || game is null) return;

        IsPerfect = r.IsPerfect;
        LeveledUp = r.LevelUp;
        NewLevel = r.Level;
        var ratio = r.Total == 0 ? 0 : (double)r.Correct / r.Total;
        Headline = r.IsPerfect ? "¡PERFECTO! 🌟"
            : ratio >= 0.7 ? "¡Muy bien! 🎉"
            : ratio >= 0.4  ? "¡Buen intento! 💪"
            : "¡Cada error enseña! 🌱";
        ScoreLabel = $"{r.Correct}/{r.Total}";
        XpLabel = $"+{r.XpEarned} XP";
        CoinsLabel = $"+{r.CoinsEarned}";
        StreakLabel = r.Streak.Current > 1 ? $"🔥 Racha: {r.Streak.Current} días" : "";

        Unlocked.Clear();
        foreach (var a in r.UnlockedAchievements) Unlocked.Add(a);

        // Repaso didáctico: para cada pregunta, la respuesta correcta y su porqué.
        Review.Clear();
        var questionsById = game.Questions.ToDictionary(q => q.Id);
        foreach (var res in r.Results)
        {
            if (!questionsById.TryGetValue(res.QuestionId, out var q)) continue;
            var correctText = q.Choices.FirstOrDefault(c => c.Id == res.CorrectChoiceId)?.Text ?? "";
            Review.Add(new ReviewItem(q.Text, correctText, null, res.WasCorrect, res.Explanation, res.FunFact));
        }
    }

    [RelayCommand]
    private Task PlayAgainAsync() => nav.GoToAsync("//home");

    [RelayCommand]
    private Task GoHomeAsync() => nav.GoToAsync("//home");

    /// <summary>Comparte el resultado estilo Wordle: cuadrícula de emojis sin spoilers.</summary>
    [RelayCommand]
    private Task ShareAsync()
    {
        if (share is null || flow.LastResult is null) return Task.CompletedTask;
        var r = flow.LastResult;
        var grid = string.Join("", r.Results.Select(x => x.WasCorrect ? "🟩" : "🟥"));
        var modo = flow.Mode == GameModes.Daily ? "el reto diario" : "una partida";
        var text = $"🧠 BrainTrain — saqué {r.Correct}/{r.Total} en {modo}\n{grid}\n¿Me ganas?";
        return share.ShareTextAsync("Mi resultado en BrainTrain", text);
    }
}
