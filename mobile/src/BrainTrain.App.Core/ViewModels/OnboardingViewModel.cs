using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

/// <summary>
/// Primera pantalla: entrar a jugar en segundos como invitado (sin fricción)
/// o iniciar sesión con una cuenta existente.
/// </summary>
public partial class OnboardingViewModel(
    ApiClient api, IDeviceIdentity device, INavigationService nav) : ObservableObject
{
    public static readonly string[] AvatarCodes =
        ["fox", "owl", "cat", "panda", "robot", "alien", "lion", "penguin", "koala", "dragon"];

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _selectedAvatar = "fox";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;

    [RelayCommand]
    private async Task PlayAsGuestAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        try
        {
            var deviceId = await device.GetDeviceIdAsync();
            var name = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim();
            await api.GuestLoginAsync(deviceId, name);
            if (!string.IsNullOrEmpty(SelectedAvatar))
                await api.UpdateProfileAsync(new UpdateProfileRequest(null, SelectedAvatar));
            await nav.GoToAsync("//home");
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión. Revisa tu internet."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task GoToLoginAsync() => nav.GoToAsync("auth");
}
