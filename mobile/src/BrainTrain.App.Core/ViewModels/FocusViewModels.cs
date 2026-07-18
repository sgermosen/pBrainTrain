using System.Collections.ObjectModel;
using BrainTrain.App.Core.Focus;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

// ------------------------------------------------------------------- Hub
public partial class FocusHubViewModel(INavigationService nav) : ObservableObject
{
    [RelayCommand] private Task GoWorkAsync() => nav.GoToAsync("focustimer");
    [RelayCommand] private Task GoBreatheAsync() => nav.GoToAsync("breathe");
    [RelayCommand] private Task GoScienceAsync() => nav.GoToAsync("focusscience");
}

// ------------------------------------------------- Bloque de foco (flow)
/// <summary>
/// Bloque de concentración estilo "deep work": una meta, sin interrupciones,
/// duración configurable y sonido de fondo opcional. Basado en la evidencia
/// del coste de interrupciones (Mark 2008) y las condiciones de flow.
/// </summary>
public partial class FocusTimerViewModel : ObservableObject, IDisposable
{
    public static readonly int[] DurationOptions = [15, 25, 50, 90];

    private readonly ApiClient _api;
    private readonly IFocusAudioPlayer _audio;
    private System.Timers.Timer? _timer;
    private DateTime _startedUtc;
    private int _totalSeconds;
    private int _secondsLeft;

    public FocusTimerViewModel(ApiClient api, IFocusAudioPlayer audio, INavigationService nav)
    {
        _api = api;
        _audio = audio;
        Nav = nav;
    }

    public INavigationService Nav { get; }
    public IReadOnlyList<FocusSound> Sounds => FocusSounds.All;
    public ObservableCollection<int> Durations { get; } = [.. DurationOptions];

    [ObservableProperty] private int _selectedMinutes = 25;
    [ObservableProperty] private FocusSound _selectedSound = FocusSounds.All[0];
    [ObservableProperty] private string _goal = string.Empty;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isFinished;
    [ObservableProperty] private string _clock = "25:00";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private FocusResultDto? _result;
    [ObservableProperty] private string? _error;

    public string SelectionSummary =>
        $"{SelectedMinutes} min · {SelectedSound.Emoji} {SelectedSound.Name}";

    partial void OnSelectedMinutesChanged(int value)
    {
        Clock = $"{value:00}:00";
        OnPropertyChanged(nameof(SelectionSummary));
    }

    partial void OnSelectedSoundChanged(FocusSound value) =>
        OnPropertyChanged(nameof(SelectionSummary));

    [RelayCommand]
    private void PickDuration(int minutes) => SelectedMinutes = minutes;

    [RelayCommand]
    private void PickSound(FocusSound sound) => SelectedSound = sound;

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning) return;
        _totalSeconds = SelectedMinutes * 60;
        _secondsLeft = _totalSeconds;
        _startedUtc = DateTime.UtcNow;
        IsFinished = false;
        Result = null;
        Error = null;
        IsRunning = true;
        UpdateClock();

        if (SelectedSound.Asset is not null)
            await _audio.StartLoopAsync(SelectedSound.Asset);

        _timer?.Dispose();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (_, _) => OnTick();
        _timer.Start();
    }

    private void OnTick()
    {
        _secondsLeft--;
        UpdateClock();
        if (_secondsLeft <= 0)
            _ = CompleteAsync();
    }

    private void UpdateClock()
    {
        var s = Math.Max(0, _secondsLeft);
        Clock = $"{s / 60:00}:{s % 60:00}";
        Progress = _totalSeconds == 0 ? 0 : 1 - (double)s / _totalSeconds;
    }

    private async Task CompleteAsync()
    {
        _timer?.Stop();
        IsRunning = false;
        await _audio.StopAsync();
        await _audio.PlayChimeAsync();
        try
        {
            Result = await _api.CompleteFocusAsync("work",
                (int)(DateTime.UtcNow - _startedUtc).TotalSeconds);
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión: la sesión no se registró (el foco cuenta igual 😉)."; }
        IsFinished = true;
    }

    /// <summary>Cancela sin registrar (sesión abandonada — sin culpa, sin XP).</summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        _timer?.Stop();
        IsRunning = false;
        await _audio.StopAsync();
        await Nav.GoBackAsync();
    }

    [RelayCommand]
    private Task ExitAsync() => Nav.GoBackAsync();

    public void Dispose()
    {
        _timer?.Dispose();
        _ = _audio.StopAsync();
    }
}

// --------------------------------------------- Respiración guiada / NSDR
public partial class BreathingViewModel : ObservableObject, IDisposable
{
    private readonly ApiClient _api;
    private readonly IFocusAudioPlayer _audio;
    private readonly INavigationService _nav;
    private BreathingEngine? _engine;
    private System.Timers.Timer? _timer;
    private double _elapsed;
    private double _pacerScale = 0.35;

    public BreathingViewModel(ApiClient api, IFocusAudioPlayer audio, INavigationService nav)
    {
        _api = api;
        _audio = audio;
        _nav = nav;
    }

    public IReadOnlyList<BreathProtocol> Protocols => BreathProtocols.All;

    [ObservableProperty] private BreathProtocol? _selected;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isFinished;
    [ObservableProperty] private string _phaseLabel = "";
    [ObservableProperty] private string _promptText = "";
    [ObservableProperty] private double _pacer = 0.35;
    [ObservableProperty] private string _clock = "";
    [ObservableProperty] private bool _rainEnabled;
    [ObservableProperty] private FocusResultDto? _result;
    [ObservableProperty] private string? _error;

    public bool HasPrompts => Selected?.Prompts is not null;

    [RelayCommand]
    private void Select(BreathProtocol protocol)
    {
        Selected = protocol;
        OnPropertyChanged(nameof(HasPrompts));
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (Selected is null || IsRunning) return;
        _engine = new BreathingEngine(Selected, Selected.DefaultMinutes);
        _elapsed = 0;
        _pacerScale = 0.35;
        IsFinished = false;
        Result = null;
        Error = null;
        IsRunning = true;

        if (RainEnabled)
            await _audio.StartLoopAsync("focus/rain.wav");

        _timer?.Dispose();
        _timer = new System.Timers.Timer(100); // 10 fps: pacer fluido sin gastar batería
        _timer.Elapsed += (_, _) => OnTick();
        _timer.Start();
    }

    private void OnTick()
    {
        if (_engine is null) return;
        _elapsed += 0.1;

        var (phase, fraction, _) = _engine.PhaseAt(_elapsed);
        _pacerScale = BreathingEngine.PacerScale(phase, fraction, _pacerScale);
        Pacer = _pacerScale;
        PhaseLabel = phase.Label;
        PromptText = _engine.PromptAt(_elapsed)?.Text ?? "";

        var left = (int)Math.Max(0, _engine.TotalSeconds - _elapsed);
        Clock = $"{left / 60:00}:{left % 60:00}";

        if (_engine.IsComplete(_elapsed))
            _ = CompleteAsync();
    }

    private async Task CompleteAsync()
    {
        _timer?.Stop();
        IsRunning = false;
        await _audio.StopAsync();
        await _audio.PlayChimeAsync();
        try
        {
            Result = await _api.CompleteFocusAsync(
                Selected?.Code == "nsdr" ? "reset" : "calm", (int)_elapsed);
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión: la sesión no se registró."; }
        IsFinished = true;
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        _timer?.Stop();
        IsRunning = false;
        await _audio.StopAsync();
        Selected = null;
        OnPropertyChanged(nameof(HasPrompts));
    }

    [RelayCommand]
    private Task ExitAsync() => _nav.GoBackAsync();

    public void Dispose()
    {
        _timer?.Dispose();
        _ = _audio.StopAsync();
    }
}
