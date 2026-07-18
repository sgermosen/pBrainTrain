using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class RubikGuidePage : ContentPage
{
    private readonly RubikGuideViewModel _vm;

    public RubikGuidePage(RubikGuideViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Cleanup();
    }
}
