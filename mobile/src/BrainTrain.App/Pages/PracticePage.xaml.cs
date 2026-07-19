using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class PracticePage : ContentPage
{
    private readonly PracticeViewModel _vm;

    public PracticePage(PracticeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnClose(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
