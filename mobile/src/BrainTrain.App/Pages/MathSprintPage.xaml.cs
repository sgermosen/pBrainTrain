using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class MathSprintPage : ContentPage
{
    private readonly MathSprintViewModel _vm;

    public MathSprintPage(MathSprintViewModel vm)
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
