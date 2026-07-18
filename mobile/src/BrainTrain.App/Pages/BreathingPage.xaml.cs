using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class BreathingPage : ContentPage
{
    private readonly BreathingViewModel _vm;

    public BreathingPage(BreathingViewModel vm)
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
