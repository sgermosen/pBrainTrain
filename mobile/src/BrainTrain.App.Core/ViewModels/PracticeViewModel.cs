using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

public sealed partial class PracticeChoice(ChoiceDto dto) : ObservableObject
{
    public int Id => dto.Id;
    public string Text => dto.Text;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _showCorrect;
    [ObservableProperty] private bool _showWrong;
}

/// <summary>
/// Práctica offline: preguntas cacheadas localmente CON respuesta, así que hay
/// feedback instantáneo por pregunta y funciona sin internet. No otorga XP —
/// es puro aprendizaje (y por eso no hay nada que hackear).
/// </summary>
public partial class PracticeViewModel(ApiClient api, IAppPreferences prefs) : ObservableObject
{
    public const string PackKey = "practice.pack";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private List<PracticeQuestionDto> _pool = [];
    private PracticeQuestionDto? _current;
    private readonly Random _rng = Random.Shared;

    public ObservableCollection<PracticeChoice> Choices { get; } = [];

    [ObservableProperty] private string _questionText = "";
    [ObservableProperty] private string? _imageUrl;
    [ObservableProperty] private bool _answered;
    [ObservableProperty] private bool _wasCorrect;
    [ObservableProperty] private string _explanation = "";
    [ObservableProperty] private string? _funFact;
    [ObservableProperty] private int _correctCount;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string? _error;

    /// <summary>Refresca el pack si hay conexión; si no, usa el caché local.</summary>
    public async Task LoadAsync()
    {
        try
        {
            var pack = await api.GetPracticePackAsync();
            prefs.Set(PackKey, JsonSerializer.Serialize(pack, Json));
            _pool = pack.Questions;
            IsOffline = false;
        }
        catch (Exception e) when (e is HttpRequestException or ApiException or TaskCanceledException)
        {
            var cached = prefs.Get(PackKey);
            if (cached is not null)
            {
                _pool = JsonSerializer.Deserialize<PracticePackDto>(cached, Json)?.Questions ?? [];
                IsOffline = true;
            }
            else
            {
                Error = "Necesitas conexión la primera vez para descargar el pack de práctica.";
                return;
            }
        }

        CorrectCount = 0;
        TotalCount = 0;
        Next();
    }

    private void Next()
    {
        if (_pool.Count == 0) return;
        _current = _pool[_rng.Next(_pool.Count)];
        QuestionText = _current.Text;
        ImageUrl = _current.ImageUrl is null ? null : api.BaseUrl + _current.ImageUrl;
        Answered = false;
        Explanation = "";
        FunFact = null;
        Choices.Clear();
        foreach (var c in _current.Choices)
            Choices.Add(new PracticeChoice(c));
    }

    [RelayCommand]
    private void Answer(PracticeChoice choice)
    {
        if (Answered || _current is null) return;
        Answered = true;
        TotalCount++;
        WasCorrect = choice.Id == _current.CorrectChoiceId;
        if (WasCorrect) CorrectCount++;

        choice.IsSelected = true;
        foreach (var c in Choices)
        {
            if (c.Id == _current.CorrectChoiceId) c.ShowCorrect = true;
            else if (c.IsSelected) c.ShowWrong = true;
        }
        Explanation = _current.Explanation;
        FunFact = _current.FunFact;
    }

    [RelayCommand]
    private void NextQuestion() => Next();
}
