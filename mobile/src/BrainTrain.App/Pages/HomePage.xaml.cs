using BrainTrain.App.Core;
using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        if (_vm.Profile is not null)
            AvatarButton.Text = Avatars.Emoji(_vm.Profile.AvatarCode);
    }
}
