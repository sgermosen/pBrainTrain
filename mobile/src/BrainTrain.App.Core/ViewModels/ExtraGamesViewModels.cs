using System.Collections.ObjectModel;
using System.Diagnostics;
using BrainTrain.App.Core.Minigames;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

// ------------------------------------------------------ Parejas de Memoria
public sealed partial class CardItem(int index, string emoji) : ObservableObject
{
    public int Index { get; } = index;
    public string Emoji { get; } = emoji;
    [ObservableProperty] private bool _isFaceUp;
    [ObservableProperty] private bool _isMatched;
    public string Display => IsFaceUp || IsMatched ? Emoji : "❔";
    partial void OnIsFaceUpChanged(bool value) => OnPropertyChanged(nameof(Display));
    partial void OnIsMatchedChanged(bool value) => OnPropertyChanged(nameof(Display));
}

public partial class MemoryPairsViewModel(ApiClient api, INavigationService nav)
    : MinigameViewModelBase(api, nav)
{
    private readonly MemoryPairsEngine _engine = new();

    public ObservableCollection<CardItem> Cards { get; } = [];
    [ObservableProperty] private int _moves;
    [ObservableProperty] private int _pairs;

    public void Start()
    {
        _engine.Reset();
        MarkStarted();
        Cards.Clear();
        for (var i = 0; i < _engine.Faces.Count; i++)
            Cards.Add(new CardItem(i, _engine.Faces[i]));
        Moves = 0;
        Pairs = 0;
    }

    [RelayCommand]
    private async Task FlipAsync(CardItem card)
    {
        var result = _engine.Flip(card.Index);
        if (result == FlipResult.Ignored) return;

        card.IsFaceUp = true;
        Moves = _engine.Moves;

        switch (result)
        {
            case FlipResult.Matched:
                foreach (var c in Cards.Where(c => c.IsFaceUp && !c.IsMatched))
                {
                    c.IsMatched = true;
                    c.IsFaceUp = false;
                }
                Pairs = _engine.MatchedPairs;
                if (_engine.IsComplete)
                    await SubmitAsync("memory_pairs", _engine.MatchedPairs);
                break;

            case FlipResult.Mismatch:
                await Task.Delay(750); // deja ver el fallo: así se memoriza
                var pending = _engine.ClearPending();
                if (pending is { } p)
                {
                    Cards[p.A].IsFaceUp = false;
                    Cards[p.B].IsFaceUp = false;
                }
                break;
        }
    }
}

// ---------------------------------------------------------------- Simón Dice
public sealed partial class SimonPad(int index, string color) : ObservableObject
{
    public int Index { get; } = index;
    public string Color { get; } = color;
    [ObservableProperty] private bool _isLit;
}

public partial class SimonViewModel(ApiClient api, INavigationService nav)
    : MinigameViewModelBase(api, nav)
{
    private readonly SimonEngine _engine = new();

    public ObservableCollection<SimonPad> Pads { get; } =
        [new(0, "#EF5350"), new(1, "#66BB6A"), new(2, "#42A5F5"), new(3, "#FFCA28")];

    [ObservableProperty] private int _round;
    [ObservableProperty] private string _statusText = "Observa la secuencia…";
    [ObservableProperty] private bool _inputEnabled;
    [ObservableProperty] private bool _isPlaying;

    [RelayCommand]
    public async Task StartAsync()
    {
        _engine.Reset();
        MarkStarted();
        Round = 0;
        IsPlaying = true;
        await PlaybackAsync();
    }

    private async Task PlaybackAsync()
    {
        InputEnabled = false;
        StatusText = $"Ronda {_engine.Sequence.Count} — observa…";
        await Task.Delay(600);
        foreach (var color in _engine.Sequence)
        {
            Pads[color].IsLit = true;
            await Task.Delay(420);
            Pads[color].IsLit = false;
            await Task.Delay(160);
        }
        StatusText = "¡Tu turno!";
        InputEnabled = true;
    }

    [RelayCommand]
    private async Task TapAsync(SimonPad pad)
    {
        if (!InputEnabled || _engine.IsGameOver) return;

        pad.IsLit = true;
        await Task.Delay(140);
        pad.IsLit = false;

        if (!_engine.Input(pad.Index, out var roundComplete))
        {
            InputEnabled = false;
            IsPlaying = false;
            StatusText = $"¡Fin! Llegaste a la ronda {_engine.CompletedRounds}.";
            await SubmitAsync("simon", _engine.CompletedRounds);
            return;
        }

        if (roundComplete)
        {
            Round = _engine.CompletedRounds;
            _engine.Extend();
            await PlaybackAsync();
        }
    }
}

// -------------------------------------------- Encuentra las Diferencias
public sealed record FoundMarker(double X, double Y);

public partial class SpotDiffViewModel(ApiClient api, INavigationService nav)
    : MinigameViewModelBase(api, nav)
{
    private readonly SpotDiffEngine _engine = new();

    public ObservableCollection<FoundMarker> Markers { get; } = [];
    [ObservableProperty] private string _imageA = "";
    [ObservableProperty] private string _imageB = "";
    [ObservableProperty] private string _sceneLabel = "";
    [ObservableProperty] private string _foundLabel = "";
    [ObservableProperty] private bool _sceneComplete;
    [ObservableProperty] private bool _hasNextScene;
    [ObservableProperty] private bool _missFlash;

    public void Start()
    {
        _engine.Reset();
        MarkStarted();
        LoadScene();
    }

    private void LoadScene()
    {
        var s = _engine.Current;
        ImageA = s.ImageA;
        ImageB = s.ImageB;
        Markers.Clear();
        SceneComplete = false;
        HasNextScene = !_engine.IsLastScene;
        SceneLabel = $"{s.Name} · escena {_engine.SceneNumber}/{_engine.SceneCount}";
        UpdateFound();
    }

    private void UpdateFound() =>
        FoundLabel = $"{_engine.FoundInScene.Count}/{_engine.Current.Diffs.Count} diferencias";

    /// <summary>Toque normalizado (0..1) sobre cualquiera de las dos imágenes.</summary>
    public async Task TapAsync(double x, double y)
    {
        if (IsFinished || SceneComplete) return;

        var hit = _engine.TryTap(x, y);
        if (hit is null)
        {
            MissFlash = true;
            await Task.Delay(250);
            MissFlash = false;
            return;
        }

        var d = _engine.Current.Diffs[hit.Value];
        Markers.Add(new FoundMarker(d.X, d.Y));
        UpdateFound();

        if (_engine.SceneComplete)
        {
            SceneComplete = true;
            if (_engine.IsLastScene)
                await SubmitAsync("spot_diff", _engine.Score);
        }
    }

    [RelayCommand]
    private void NextScene()
    {
        if (_engine.NextScene())
            LoadScene();
    }

    [RelayCommand]
    private Task FinishAsync() => SubmitAsync("spot_diff", _engine.Score);
}

// --------------------------------------------------- Guía del Cubo de Rubik
public partial class RubikGuideViewModel(ApiClient api, INavigationService nav, IAppPreferences prefs)
    : MinigameViewModelBase(api, nav)
{
    private const string BestKey = "rubik.best";
    private readonly Stopwatch _solveWatch = new();
    private readonly List<double> _times = [];
    private System.Timers.Timer? _ticker;

    public IReadOnlyList<RubikStep> Steps => RubikGuide.Steps;

    [ObservableProperty] private int _stepIndex;
    [ObservableProperty] private bool _showTimer;
    [ObservableProperty] private string _timerDisplay = "0.00";
    [ObservableProperty] private bool _timerRunning;
    [ObservableProperty] private string _bestDisplay = "—";
    [ObservableProperty] private string _ao5Display = "—";

    public RubikStep Current => Steps[StepIndex];
    public bool IsFirst => StepIndex == 0;
    public bool IsLast => StepIndex == Steps.Count - 1;
    public string ProgressLabel => $"{StepIndex + 1} de {Steps.Count}";
    public bool HasPattern => Current.FacePattern is not null;
    public IReadOnlyList<string> PatternCells =>
        Current.FacePattern?.Select(c => c.ToString()).ToList() ?? [];

    public void Start()
    {
        MarkStarted();
        StepIndex = 0;
        if (double.TryParse(prefs.Get(BestKey), out var best))
            BestDisplay = $"{best:0.00}s";
    }

    partial void OnStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(IsFirst));
        OnPropertyChanged(nameof(IsLast));
        OnPropertyChanged(nameof(ProgressLabel));
        OnPropertyChanged(nameof(HasPattern));
        OnPropertyChanged(nameof(PatternCells));
    }

    [RelayCommand] private void Next() { if (!IsLast) StepIndex++; }
    [RelayCommand] private void Prev() { if (!IsFirst) StepIndex--; }
    [RelayCommand] private void ToggleTimer() => ShowTimer = !ShowTimer;

    /// <summary>Al terminar la guía: +20 XP (una lectura real toma >1 min; lo valida el servidor).</summary>
    [RelayCommand]
    private Task CompleteGuideAsync() => SubmitAsync("rubik_guide", 1);

    // ----- Timer de speedcubing: toca para iniciar, toca para parar -----
    [RelayCommand]
    private void TimerTap()
    {
        if (!TimerRunning)
        {
            _solveWatch.Restart();
            TimerRunning = true;
            _ticker?.Dispose();
            _ticker = new System.Timers.Timer(50);
            _ticker.Elapsed += (_, _) => TimerDisplay = $"{_solveWatch.Elapsed.TotalSeconds:0.00}";
            _ticker.Start();
            return;
        }

        _solveWatch.Stop();
        _ticker?.Stop();
        TimerRunning = false;
        var seconds = _solveWatch.Elapsed.TotalSeconds;
        TimerDisplay = $"{seconds:0.00}";
        _times.Add(seconds);

        if (!double.TryParse(prefs.Get(BestKey), out var best) || seconds < best)
        {
            prefs.Set(BestKey, seconds.ToString("0.00"));
            BestDisplay = $"{seconds:0.00}s 🎉";
        }

        // Ao5 estándar: promedio de las últimas 5 quitando mejor y peor.
        if (_times.Count >= 5)
        {
            var last5 = _times.TakeLast(5).OrderBy(t => t).Skip(1).SkipLast(1);
            Ao5Display = $"{last5.Average():0.00}s";
        }
    }

    public void Cleanup() => _ticker?.Dispose();
}
