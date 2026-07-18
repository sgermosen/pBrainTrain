using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class ChainCalcPage : ContentPage
{
    private readonly ChainCalcViewModel _vm;

    public ChainCalcPage(ChainCalcViewModel vm)
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
