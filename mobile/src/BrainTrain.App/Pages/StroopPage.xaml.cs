using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class StroopPage : ContentPage
{
    private readonly StroopViewModel _vm;

    public StroopPage(StroopViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Dispose();
    }
}
