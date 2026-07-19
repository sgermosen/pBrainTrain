using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

/// <summary>
/// Pantalla principal: estado del jugador (vidas, racha, nivel), reto del día
/// destacado, juego rápido y categorías.
/// </summary>
public partial class HomeViewModel(
    ApiClient api, GameFlow flow, INavigationService nav, ISfxPlayer? sfx = null) : ObservableObject
{
    [ObservableProperty] private ProfileDto? _profile;
    [ObservableProperty] private DailyStatusDto? _daily;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _questMessage;

    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public ObservableCollection<QuestDto> Quests { get; } = [];

    public bool NeedsCalibration => Profile is { Calibrated: false };
    public string LeagueLabel => Profile is null ? "" : $"🛡️ Liga {Profile.LeagueName}";

    public bool DailyCompleted => Daily?.Completed == true;
    public string StreakLabel => Profile is null ? "" :
        Profile.Streak.Current > 0 ? $"🔥 {Profile.Streak.Current} días" : "Empieza tu racha hoy";
    public string LivesLabel => Profile is null ? "" : $"{Profile.Lives.Current}/{Profile.Lives.Max}";
    public double LevelProgress => Profile is null || Profile.XpForNextLevel == 0
        ? 0 : Math.Clamp((double)Profile.XpIntoLevel / Profile.XpForNextLevel, 0, 1);

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        try
        {
            Profile = await api.GetProfileAsync();
            Daily = await api.GetDailyStatusAsync();
            if (Categories.Count == 0)
            {
                foreach (var c in await api.GetCategoriesAsync())
                    Categories.Add(c);
            }
            Quests.Clear();
            foreach (var q in await api.GetQuestsAsync())
                Quests.Add(q);
            OnPropertyChanged(nameof(DailyCompleted));
            OnPropertyChanged(nameof(StreakLabel));
            OnPropertyChanged(nameof(LivesLabel));
            OnPropertyChanged(nameof(LevelProgress));
            OnPropertyChanged(nameof(NeedsCalibration));
            OnPropertyChanged(nameof(LeagueLabel));
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión. Revisa tu internet."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task PlayQuickAsync() => StartGameAsync(GameModes.Quick, null);

    [RelayCommand]
    private Task PlayDailyAsync() =>
        DailyCompleted ? Task.CompletedTask : StartGameAsync(GameModes.Daily, null);

    [RelayCommand]
    private Task PlayCategoryAsync(CategoryDto category) =>
        StartGameAsync(GameModes.Category, category);

    /// <summary>Test inicial de nivel: 10 preguntas gratis que calibran la dificultad.</summary>
    [RelayCommand]
    private Task PlayCalibrationAsync() => StartGameAsync(GameModes.Calibration, null);

    /// <summary>Reclama el cofre de una misión completada.</summary>
    [RelayCommand]
    private async Task ClaimQuestAsync(QuestDto quest)
    {
        if (!quest.Completed || quest.Claimed) return;
        try
        {
            var result = await api.ClaimQuestAsync(quest.Code);
            Profile = result.Profile;
            _ = sfx?.PlayAsync(Sfx.Coin);
            QuestMessage = $"🎁 +{result.CoinsGranted} 🪙 y +{result.XpGranted} XP";
            var i = Quests.IndexOf(quest);
            if (i >= 0) Quests[i] = quest with { Claimed = true };
            OnPropertyChanged(nameof(LivesLabel));
            OnPropertyChanged(nameof(LevelProgress));
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
    }

    private async Task StartGameAsync(string mode, CategoryDto? category)
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        try
        {
            var start = await api.StartGameAsync(mode, category?.Id);
            flow.CurrentGame = start;
            flow.Mode = mode;
            flow.Category = category;
            flow.LastResult = null;
            await nav.GoToAsync("quiz");
        }
        catch (ApiException e) when (e.Code == "no_lives")
        {
            // Sin combustible: llévalo a la tienda con contexto, no con frustración.
            Error = e.Message;
            await nav.GoToAsync("store");
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión. Revisa tu internet."; }
        finally { IsBusy = false; }
    }

    [RelayCommand] private Task GoTrainingAsync() => nav.GoToAsync("training");
    [RelayCommand] private Task GoDuelsAsync() => nav.GoToAsync("duels");
    [RelayCommand] private Task GoPracticeAsync() => nav.GoToAsync("practice");
    [RelayCommand] private Task GoFocusAsync() => nav.GoToAsync("focus");
    [RelayCommand] private Task GoProfileAsync() => nav.GoToAsync("profile");
    [RelayCommand] private Task GoStoreAsync() => nav.GoToAsync("store");
    [RelayCommand] private Task GoLeaderboardAsync() => nav.GoToAsync("leaderboard");
    [RelayCommand] private Task GoAchievementsAsync() => nav.GoToAsync("achievements");
    [RelayCommand] private Task GoSettingsAsync() => nav.GoToAsync("settings");
}
