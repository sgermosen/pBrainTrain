using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class DuelsPage : ContentPage
{
    private readonly DuelsViewModel _vm;

    public DuelsPage(DuelsViewModel vm)
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
