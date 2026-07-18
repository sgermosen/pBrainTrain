using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class NBackPage : ContentPage
{
    private readonly NBackViewModel _vm;

    public NBackPage(NBackViewModel vm)
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
