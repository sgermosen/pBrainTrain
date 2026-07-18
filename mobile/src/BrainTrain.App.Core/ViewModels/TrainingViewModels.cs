using System.Collections.ObjectModel;
using BrainTrain.App.Core.Minigames;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

// ------------------------------------------------------------ Entrenamiento
public partial class TrainingViewModel(ApiClient api, INavigationService nav) : ObservableObject
{
    public ObservableCollection<MinigameDto> Games { get; } = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (Games.Count > 0) return;
        IsBusy = true;
        try
        {
            foreach (var g in await api.GetMinigamesAsync())
                Games.Add(g);
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task OpenAsync(MinigameDto game) => nav.GoToAsync(game.Code switch
    {
        "g2048" => "game2048",
        "math_sprint" => "mathsprint",
        "word_search" => "wordsearch",
        "memory_pairs" => "memorypairs",
        "simon" => "simon",
        "spot_diff" => "spotdiff",
        "rubik_guide" => "rubikguide",
        "stroop" => "stroop",
        "chain30" => "chaincalc",
        "nback" => "nback",
        _ => "training"
    });
}

/// <summary>Base común: cronómetro de sesión y envío del resultado al servidor.</summary>
public abstract partial class MinigameViewModelBase(ApiClient api, INavigationService nav) : ObservableObject
{
    protected readonly ApiClient Api = api;
    protected readonly INavigationService Nav = nav;
    private DateTime _startedUtc;

    [ObservableProperty] private bool _isFinished;
    [ObservableProperty] private bool _isSubmitting;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private MinigameResultDto? _result;

    protected void MarkStarted()
    {
        _startedUtc = DateTime.UtcNow;
        IsFinished = false;
        Result = null;
        Error = null;
    }

    protected int ElapsedMs => (int)Math.Min(int.MaxValue, (DateTime.UtcNow - _startedUtc).TotalMilliseconds);

    protected async Task SubmitAsync(string code, int score)
    {
        if (IsSubmitting || Result is not null) return;
        IsSubmitting = true;
        try
        {
            Result = await Api.SubmitMinigameAsync(code, score, ElapsedMs);
            IsFinished = true;
        }
        catch (ApiException e) { Error = e.Message; IsFinished = true; }
        catch (HttpRequestException) { Error = "Sin conexión: el resultado no se pudo enviar."; IsFinished = true; }
        finally { IsSubmitting = false; }
    }

    [RelayCommand]
    protected Task ExitAsync() => Nav.GoBackAsync();
}

// ------------------------------------------------------------------- 2048
public sealed partial class TileCell : ObservableObject
{
    [ObservableProperty] private int _value;
    public string Display => Value == 0 ? "" : Value.ToString();
    partial void OnValueChanged(int value) => OnPropertyChanged(nameof(Display));
}

public partial class Game2048ViewModel(ApiClient api, INavigationService nav)
    : MinigameViewModelBase(api, nav)
{
    private readonly Game2048Engine _engine = new();

    public ObservableCollection<TileCell> Cells { get; } =
        [.. Enumerable.Range(0, 16).Select(_ => new TileCell())];

    [ObservableProperty] private int _score;

    public void Start()
    {
        _engine.Reset();
        MarkStarted();
        Refresh();
    }

    [RelayCommand]
    private async Task MoveAsync(string direction)
    {
        if (IsFinished) return;
        var dir = direction switch
        {
            "up" => MoveDirection.Up,
            "down" => MoveDirection.Down,
            "left" => MoveDirection.Left,
            _ => MoveDirection.Right
        };
        if (!_engine.Move(dir)) return;

        Refresh();
        if (_engine.IsGameOver())
            await SubmitAsync("g2048", _engine.Score);
    }

    /// <summary>2048 puede ser infinito: el jugador puede cobrar su XP cuando quiera.</summary>
    [RelayCommand]
    private Task CashOutAsync() => SubmitAsync("g2048", _engine.Score);

    private void Refresh()
    {
        for (var i = 0; i < Cells.Count; i++)
            Cells[i].Value = _engine.Cells[i];
        Score = _engine.Score;
    }
}

// ---------------------------------------------------------- Cálculo Rápido
public partial class MathSprintViewModel : MinigameViewModelBase, IDisposable
{
    public const int GameSeconds = 60;

    private readonly MathSprintEngine _engine = new();
    private System.Timers.Timer? _timer;
    private int _secondsLeft;

    public MathSprintViewModel(ApiClient api, INavigationService nav) : base(api, nav) { }

    public ObservableCollection<int> Options { get; } = [];
    [ObservableProperty] private string _problemText = "";
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
        ShowProblem();

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
            _ = SubmitAsync("math_sprint", _engine.Correct);
        }
    }

    [RelayCommand]
    private void Answer(int option)
    {
        if (!IsRunning) return;
        _engine.Answer(option);
        Score = _engine.Correct;
        ShowProblem();
    }

    private void ShowProblem()
    {
        ProblemText = _engine.Current.Text;
        Options.Clear();
        foreach (var o in _engine.Current.Options) Options.Add(o);
    }

    public void Dispose() => _timer?.Dispose();
}

// ---------------------------------------------------------- Sopa de Letras
public sealed partial class LetterCell(int row, int col, char letter) : ObservableObject
{
    public int Row { get; } = row;
    public int Col { get; } = col;
    public string Letter { get; } = letter.ToString();
    [ObservableProperty] private bool _isAnchor;
    [ObservableProperty] private bool _isFound;
}

public sealed partial class WordItem(string word) : ObservableObject
{
    public string Word { get; } = word;
    [ObservableProperty] private bool _isFound;
}

public partial class WordSearchViewModel(ApiClient api, INavigationService nav)
    : MinigameViewModelBase(api, nav)
{
    private readonly WordSearchEngine _engine = new();
    private LetterCell? _anchor;

    public ObservableCollection<LetterCell> Cells { get; } = [];
    public ObservableCollection<WordItem> Words { get; } = [];
    [ObservableProperty] private int _foundCount;
    [ObservableProperty] private int _totalWords;

    public void Start()
    {
        _engine.Generate();
        MarkStarted();
        _anchor = null;

        Cells.Clear();
        for (var r = 0; r < WordSearchEngine.Size; r++)
            for (var c = 0; c < WordSearchEngine.Size; c++)
                Cells.Add(new LetterCell(r, c, _engine[r, c]));

        Words.Clear();
        foreach (var w in _engine.Words) Words.Add(new WordItem(w));
        FoundCount = 0;
        TotalWords = _engine.Words.Count;
    }

    /// <summary>Selección en dos toques: primero el inicio, luego el final de la palabra.</summary>
    [RelayCommand]
    private async Task TapAsync(LetterCell cell)
    {
        if (IsFinished) return;

        if (_anchor is null || ReferenceEquals(_anchor, cell))
        {
            if (_anchor is not null) _anchor.IsAnchor = false;
            _anchor = cell;
            cell.IsAnchor = true;
            return;
        }

        var found = _engine.TrySelect(_anchor.Row, _anchor.Col, cell.Row, cell.Col);
        _anchor.IsAnchor = false;
        _anchor = null;
        if (found is null) return;

        foreach (var (r, c) in found)
            Cells[r * WordSearchEngine.Size + c].IsFound = true;
        foreach (var w in Words.Where(w => !w.IsFound && _engine.Found.Contains(w.Word)))
            w.IsFound = true;
        FoundCount = _engine.Score;

        if (_engine.IsComplete)
            await SubmitAsync("word_search", _engine.Score);
    }

    /// <summary>Cobrar lo encontrado sin completar todo.</summary>
    [RelayCommand]
    private Task FinishAsync() => SubmitAsync("word_search", _engine.Score);
}
