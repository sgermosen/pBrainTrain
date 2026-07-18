using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class WordSearchPage : ContentPage
{
    private readonly WordSearchViewModel _vm;

    public WordSearchPage(WordSearchViewModel vm)
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
