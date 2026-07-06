using BrainTrain.App.Core;
using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _vm;

    public OnboardingPage(OnboardingViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        BuildAvatarRow();
    }

    private void BuildAvatarRow()
    {
        foreach (var code in Avatars.Codes)
        {
            var button = new Button
            {
                Text = Avatars.Emoji(code),
                FontSize = 26,
                WidthRequest = 56,
                HeightRequest = 56,
                Margin = 4,
                CornerRadius = 28,
                BackgroundColor = Colors.Transparent
            };
            button.Clicked += (_, _) =>
            {
                _vm.SelectedAvatar = code;
                HighlightSelection();
            };
            AvatarRow.Children.Add(button);
        }
        HighlightSelection();
    }

    private void HighlightSelection()
    {
        for (var i = 0; i < AvatarRow.Children.Count; i++)
        {
            var b = (Button)AvatarRow.Children[i];
            var selected = Avatars.Codes[i] == _vm.SelectedAvatar;
            b.BackgroundColor = selected
                ? (Color)Application.Current!.Resources["Secondary"]
                : Colors.Transparent;
            b.BorderColor = selected
                ? (Color)Application.Current!.Resources["Primary"]
                : Colors.Transparent;
            b.BorderWidth = selected ? 2 : 0;
        }
    }
}
