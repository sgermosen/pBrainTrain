using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class Game2048Page : ContentPage
{
    private readonly Game2048ViewModel _vm;

    public Game2048Page(Game2048ViewModel vm)
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
