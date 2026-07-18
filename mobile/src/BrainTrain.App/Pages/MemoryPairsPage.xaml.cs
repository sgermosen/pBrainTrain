using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class MemoryPairsPage : ContentPage
{
    private readonly MemoryPairsViewModel _vm;

    public MemoryPairsPage(MemoryPairsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Start();
    }
}
