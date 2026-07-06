using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class StorePage : ContentPage
{
    private readonly StoreViewModel _vm;

    public StorePage(StoreViewModel vm)
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
