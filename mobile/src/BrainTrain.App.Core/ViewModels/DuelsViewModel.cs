using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

/// <summary>
/// Duelos 1v1: crear (código para compartir), unirse por código o al azar, y
/// lista de duelos con sus resultados. La partida en sí reutiliza el quiz.
/// </summary>
public partial class DuelsViewModel(
    ApiClient api, GameFlow flow, INavigationService nav, IShareService share) : ObservableObject
{
    public ObservableCollection<DuelDto> Duels { get; } = [];

    [ObservableProperty] private string _joinCode = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _createdCode;

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            Duels.Clear();
            foreach (var d in await api.MyDuelsAsync())
                Duels.Add(d);
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
    }

    [RelayCommand]
    private Task CreateAsync() => StartAsync(() => api.CreateDuelAsync(openToPublic: false));

    [RelayCommand]
    private Task RandomAsync() => StartAsync(() => api.RandomDuelAsync());

    [RelayCommand]
    private Task JoinAsync() =>
        string.IsNullOrWhiteSpace(JoinCode)
            ? Task.CompletedTask
            : StartAsync(() => api.JoinDuelAsync(JoinCode.Trim().ToUpperInvariant()));

    private async Task StartAsync(Func<Task<DuelStartResponse>> starter)
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        try
        {
            var start = await starter();
            CreatedCode = start.Duel.Status == "WaitingOpponent" ? start.Duel.Code : null;

            // La partida del duelo se juega con el quiz normal.
            flow.CurrentGame = new StartGameResponse(start.SessionId, start.Questions, start.Lives);
            flow.Mode = GameModes.Duel;
            flow.Category = null;
            flow.LastResult = null;
            await nav.GoToAsync("quiz");
        }
        catch (ApiException e) when (e.Code == "no_lives")
        {
            Error = e.Message;
            await nav.GoToAsync("store");
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task ShareCodeAsync(DuelDto duel) => share.ShareTextAsync(
        "Duelo BrainTrain",
        $"🧠⚔️ ¡Te reto a un duelo en BrainTrain! Entra con el código {duel.Code} y demuestra tu ingenio.");

    [RelayCommand]
    private Task ExitAsync() => nav.GoBackAsync();
}
