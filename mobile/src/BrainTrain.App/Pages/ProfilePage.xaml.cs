using BrainTrain.App.Core;
using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _vm;

    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        if (_vm.Profile is not null)
            AvatarLabel.Text = Avatars.Emoji(_vm.Profile.AvatarCode);
    }
}
