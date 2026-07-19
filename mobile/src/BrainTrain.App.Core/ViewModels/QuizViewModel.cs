using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

public sealed partial class ChoiceItem(ChoiceDto dto) : ObservableObject
{
    public int Id => dto.Id;
    public string Text => dto.Text;
    [ObservableProperty] private bool _isSelected;
}

/// <summary>
/// La partida en sí: una pregunta a la vez, temporizador visible y avance con
/// confirmación ("lock-in"). La corrección la hace SOLO el servidor al final:
/// el cliente nunca conoce las respuestas correctas (anti-trampas).
/// </summary>
public partial class QuizViewModel : ObservableObject, IDisposable
{
    private readonly ApiClient _api;
    private readonly GameFlow _flow;
    private readonly INavigationService _nav;
    private readonly IHaptics _haptics;
    private QuizEngine? _engine;
    private System.Timers.Timer? _timer;
    private int _secondsLeft;

    public QuizViewModel(ApiClient api, GameFlow flow, INavigationService nav, IHaptics? haptics = null)
    {
        _api = api;
        _flow = flow;
        _nav = nav;
        _haptics = haptics ?? new NoopHaptics();
    }

    public ObservableCollection<ChoiceItem> Choices { get; } = [];

    [ObservableProperty] private string _questionText = string.Empty;
    [ObservableProperty] private string? _imageUrl;
    [ObservableProperty] private string _categoryBadge = string.Empty;
    [ObservableProperty] private string _progressLabel = string.Empty;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private int _secondsDisplay;
    [ObservableProperty] private double _timeFraction = 1;
    [ObservableProperty] private bool _isSubmitting;
    [ObservableProperty] private bool _canConfirm;
    [ObservableProperty] private string? _error;

    private int? _selectedChoiceId;

    /// <summary>Arranca con la partida cargada en GameFlow (invocado en OnAppearing).</summary>
    public void Start()
    {
        var game = _flow.CurrentGame;
        if (game is null || game.Questions.Count == 0)
            return;

        _engine = new QuizEngine(game.Questions);
        ShowCurrent();
        StartTimer();
    }

    private void ShowCurrent()
    {
        if (_engine is null || _engine.IsFinished) return;
        var q = _engine.Current;

        _selectedChoiceId = null;
        CanConfirm = false;
        Choices.Clear();
        foreach (var c in q.Choices)
            Choices.Add(new ChoiceItem(c));

        QuestionText = q.Text;
        ImageUrl = q.ImageUrl is null ? null : _api.BaseUrl + q.ImageUrl;
        CategoryBadge = new string('★', q.Difficulty);
        ProgressLabel = $"{_engine.Index + 1} de {_engine.Total}";
        Progress = _engine.Progress;
        _secondsLeft = _engine.SecondsPerQuestion;
        SecondsDisplay = _secondsLeft;
        TimeFraction = 1;
    }

    private void StartTimer()
    {
        _timer?.Dispose();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (_, _) => OnTick();
        _timer.Start();
    }

    private void OnTick()
    {
        _secondsLeft--;
        SecondsDisplay = Math.Max(0, _secondsLeft);
        TimeFraction = _engine is null ? 0 : Math.Max(0, (double)_secondsLeft / _engine.SecondsPerQuestion);
        if (_secondsLeft <= 0)
        {
            // Tiempo agotado: cuenta como no respondida y avanza.
            _ = AdvanceAsync(null);
        }
    }

    [RelayCommand]
    private void SelectChoice(ChoiceItem item)
    {
        _haptics.Click();
        foreach (var c in Choices) c.IsSelected = ReferenceEquals(c, item);
        _selectedChoiceId = item.Id;
        CanConfirm = true;
    }

    [RelayCommand]
    private Task ConfirmAsync() => _selectedChoiceId is null ? Task.CompletedTask : AdvanceAsync(_selectedChoiceId);

    private async Task AdvanceAsync(int? choiceId)
    {
        if (_engine is null || IsSubmitting) return;

        _timer?.Stop();
        _engine.Answer(choiceId);

        if (!_engine.IsFinished)
        {
            ShowCurrent();
            _timer?.Start();
            return;
        }

        await SubmitAsync();
    }

    private async Task SubmitAsync()
    {
        if (_engine is null || _flow.CurrentGame is null) return;
        IsSubmitting = true;
        Error = null;
        try
        {
            var result = await _api.SubmitGameAsync(_flow.CurrentGame.SessionId, _engine.BuildSubmission());
            _flow.LastResult = result;
            await _nav.GoToAsync("results");
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión. Tu partida no se pudo enviar."; }
        finally { IsSubmitting = false; }
    }

    [RelayCommand]
    private async Task AbandonAsync()
    {
        _timer?.Stop();
        await _nav.GoBackAsync();
    }

    public void Dispose() => _timer?.Dispose();
}
