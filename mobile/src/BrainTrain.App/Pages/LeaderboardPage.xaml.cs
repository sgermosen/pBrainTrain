using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class LeaderboardPage : ContentPage
{
    private readonly LeaderboardViewModel _vm;

    public LeaderboardPage(LeaderboardViewModel vm)
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
