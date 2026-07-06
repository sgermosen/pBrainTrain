using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class ResultsPage : ContentPage
{
    private readonly ResultsViewModel _vm;

    public ResultsPage(ResultsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Load();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//home");
        return true;
    }
}
