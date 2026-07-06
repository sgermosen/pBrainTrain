using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

/// <summary>
/// Pantalla principal: estado del jugador (vidas, racha, nivel), reto del día
/// destacado, juego rápido y categorías.
/// </summary>
public partial class HomeViewModel(
    ApiClient api, GameFlow flow, INavigationService nav) : ObservableObject
{
    [ObservableProperty] private ProfileDto? _profile;
    [ObservableProperty] private DailyStatusDto? _daily;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;

    public ObservableCollection<CategoryDto> Categories { get; } = [];

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
            OnPropertyChanged(nameof(DailyCompleted));
            OnPropertyChanged(nameof(StreakLabel));
            OnPropertyChanged(nameof(LivesLabel));
            OnPropertyChanged(nameof(LevelProgress));
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

    [RelayCommand] private Task GoProfileAsync() => nav.GoToAsync("profile");
    [RelayCommand] private Task GoStoreAsync() => nav.GoToAsync("store");
    [RelayCommand] private Task GoLeaderboardAsync() => nav.GoToAsync("leaderboard");
    [RelayCommand] private Task GoAchievementsAsync() => nav.GoToAsync("achievements");
    [RelayCommand] private Task GoSettingsAsync() => nav.GoToAsync("settings");
}
