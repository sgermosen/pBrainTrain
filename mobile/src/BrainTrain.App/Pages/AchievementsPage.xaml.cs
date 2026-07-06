using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class AchievementsPage : ContentPage
{
    private readonly AchievementsViewModel _vm;

    public AchievementsPage(AchievementsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
