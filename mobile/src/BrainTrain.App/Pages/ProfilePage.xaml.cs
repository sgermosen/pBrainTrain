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
        BuildAvatarRow();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        RefreshAvatar();
    }

    private void BuildAvatarRow()
    {
        foreach (var code in Avatars.Codes)
        {
            var button = new Button
            {
                Text = Avatars.Emoji(code),
                FontSize = 24,
                WidthRequest = 52,
                HeightRequest = 52,
                Margin = 4,
                CornerRadius = 26,
                BackgroundColor = Colors.Transparent
            };
            button.Clicked += async (_, _) =>
            {
                await _vm.ChangeAvatarCommand.ExecuteAsync(code);
                RefreshAvatar();
            };
            AvatarRow.Children.Add(button);
        }
    }

    private void RefreshAvatar()
    {
        if (_vm.Profile is not null)
            AvatarLabel.Text = Avatars.Emoji(_vm.Profile.AvatarCode);
    }
}
