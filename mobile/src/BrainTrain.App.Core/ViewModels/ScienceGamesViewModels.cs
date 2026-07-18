using System.Collections.ObjectModel;
using BrainTrain.App.Core.Minigames;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

// ------------------------------------------------------------------- Stroop
public partial class StroopViewModel(ApiClient api, INavigationService nav)
    : MinigameViewModelBase(api, nav), IDisposable
{
    public const int GameSeconds = 60;

    /// <summary>Hex de la tinta por índice: rojo, verde, azul, amarillo.</summary>
    public static readonly IReadOnlyList<string> InkHex = ["#EF5350", "#66BB6A", "#42A5F5", "#FFCA28"];

    private readonly StroopEngine _engine = new();
    private System.Timers.Timer? _timer;
    private int _secondsLeft;

    [ObservableProperty] private string _wordText = "";
    [ObservableProperty] private string _inkColor = "#EF5350";
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _secondsDisplay = GameSeconds;
    [ObservableProperty] private double _timeFraction = 1;
    [ObservableProperty] private bool _isRunning;

    [RelayCommand]
    public void Start()
    {
        _engine.Start();
        MarkStarted();
        Score = 0;
        _secondsLeft = GameSeconds;
        SecondsDisplay = GameSeconds;
        TimeFraction = 1;
        IsRunning = true;
        ShowTrial();

        _timer?.Dispose();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (_, _) => OnTick();
        _timer.Start();
    }

    private void OnTick()
    {
        _secondsLeft--;
        SecondsDisplay = Math.Max(0, _secondsLeft);
        TimeFraction = Math.Max(0, (double)_secondsLeft / GameSeconds);
        if (_secondsLeft <= 0)
        {
            _timer?.Stop();
            IsRunning = false;
            _ = SubmitAsync("stroop", Score);
        }
    }

    /// <summary>Responde con el índice (0-3) del color de la tinta.</summary>
    [RelayCommand]
    private void Answer(string colorIndex)
    {
        if (!IsRunning) return;
        _engine.Answer(int.Parse(colorIndex));
        Score = Math.Max(0, _engine.Correct - _engine.Wrong); // aciertos netos
        ShowTrial();
    }

    private void ShowTrial()
    {
        WordText = _engine.Current.Word;
        InkColor = InkHex[_engine.Current.WordColorIndex];
    }

    public void Dispose() => _timer?.Dispose();
}

// ------------------------------------------------------- Cálculo en Cadena
public partial class ChainCalcViewModel(ApiClient api, INavigationService nav)
    : MinigameViewModelBase(api, nav), IDisposable
{
    public const int RoundSeconds = 30;
    public const int StepMs = 1200;

    private readonly ChainCalcEngine _engine = new();
    private System.Timers.Timer? _timer;
    private int _secondsLeft;
    private bool _roundOpen; // acepta respuesta (o timeout) de la ronda actual

    public ObservableCollection<int> Options { get; } = [];
    [ObservableProperty] private string _stepText = "";
    [ObservableProperty] private string _chainText = "";
    [ObservableProperty] private string _roundLabel = "";
    [ObservableProperty] private string _feedbackText = "";
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _secondsDisplay = RoundSeconds;
    [ObservableProperty] private double _timeFraction = 1;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _showOptions;

    [RelayCommand]
    public async Task StartAsync()
    {
        _engine.Reset();
        MarkStarted();
        Score = 0;
        IsRunning = true;
        await PlayRoundAsync();
    }

    private async Task PlayRoundAsync()
    {
        var round = _engine.NextRound();
        RoundLabel = $"Ronda {_engine.RoundNumber}/{ChainCalcEngine.TotalRounds}";
        FeedbackText = "";
        ChainText = "";
        ShowOptions = false;
        Options.Clear();

        // 30 s totales por ronda (revelado incluido).
        _secondsLeft = RoundSeconds;
        SecondsDisplay = RoundSeconds;
        TimeFraction = 1;
        _roundOpen = true;
        _timer?.Dispose();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (_, _) => OnTick();
        _timer.Start();

        // Revela la cadena por partes: cada paso se acumula sobre el anterior.
        foreach (var step in round.Steps)
        {
            StepText = step;
            ChainText = ChainText.Length == 0 ? step : $"{ChainText} {step}";
            await Task.Delay(StepMs);
        }

        StepText = "= ?";
        foreach (var o in round.Options) Options.Add(o);
        ShowOptions = true;
    }

    private void OnTick()
    {
        _secondsLeft--;
        SecondsDisplay = Math.Max(0, _secondsLeft);
        TimeFraction = Math.Max(0, (double)_secondsLeft / RoundSeconds);
        if (_secondsLeft <= 0)
        {
            _timer?.Stop();
            _ = TimeoutRoundAsync();
        }
    }

    private async Task TimeoutRoundAsync()
    {
        if (!_roundOpen) return;
        _roundOpen = false;
        ShowOptions = false;
        _engine.Answer(-1); // sin respuesta: cuenta como fallo
        FeedbackText = $"⏰ ¡Tiempo! Era {_engine.Current.Answer}";
        await NextOrFinishAsync();
    }

    [RelayCommand]
    private async Task AnswerAsync(int option)
    {
        if (!_roundOpen) return;
        _roundOpen = false;
        _timer?.Stop();
        var ok = _engine.Answer(option);
        Score = _engine.Score;
        FeedbackText = ok ? "✔ ¡Correcto!" : $"✘ Era {_engine.Current.Answer}";
        ShowOptions = false;
        await NextOrFinishAsync();
    }

    private async Task NextOrFinishAsync()
    {
        await Task.Delay(900); // deja ver el feedback
        if (_engine.IsComplete)
        {
            IsRunning = false;
            await SubmitAsync("chain30", _engine.Score);
        }
        else
        {
            await PlayRoundAsync();
        }
    }

    public void Dispose() => _timer?.Dispose();
}

// -------------------------------------------------------------- Dual N-Back
public sealed partial class NBackCell(int index) : ObservableObject
{
    public int Index { get; } = index;
    [ObservableProperty] private bool _isActive;
}

public partial class NBackViewModel(ApiClient api, INavigationService nav)
    : MinigameViewModelBase(api, nav), IDisposable
{
    public const int StimulusMs = 2000;

    private readonly NBackEngine _engine = new();
    private System.Timers.Timer? _timer;
    private bool _pressed;

    public ObservableCollection<NBackCell> Cells { get; } =
        [.. Enumerable.Range(0, NBackEngine.GridCells).Select(i => new NBackCell(i))];

    [ObservableProperty] private int _score;
    [ObservableProperty] private string _levelLabel = "1-back";
    [ObservableProperty] private string _progressLabel = "";
    [ObservableProperty] private bool _isRunning;

    [RelayCommand]
    public void Start()
    {
        _engine.Start();
        MarkStarted();
        Score = 0;
        LevelLabel = "1-back";
        IsRunning = true;
        ShowNext();

        _timer?.Dispose();
        _timer = new System.Timers.Timer(StimulusMs);
        _timer.Elapsed += (_, _) => OnTick();
        _timer.Start();
    }

    // Cierra la ventana del estímulo actual y muestra el siguiente.
    private void OnTick()
    {
        _engine.RegisterResponse(_pressed);
        Score = _engine.Correct;
        LevelLabel = $"{_engine.N}-back";

        if (_engine.IsComplete)
        {
            _timer?.Stop();
            IsRunning = false;
            _ = SubmitAsync("nback", _engine.Correct);
            return;
        }
        ShowNext();
    }

    private void ShowNext()
    {
        _pressed = false;
        var pos = _engine.Advance();
        for (var i = 0; i < Cells.Count; i++)
            Cells[i].IsActive = i == pos;
        ProgressLabel = $"Estímulo {_engine.Responses + 1}/{NBackEngine.TotalStimuli}";
    }

    /// <summary>El jugador cree que la posición coincide con la de N pasos atrás.</summary>
    [RelayCommand]
    private void Match()
    {
        if (!IsRunning) return;
        _pressed = true;
    }

    public void Dispose() => _timer?.Dispose();
}
