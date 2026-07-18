using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class TrainingPage : ContentPage
{
    private readonly TrainingViewModel _vm;

    public TrainingPage(TrainingViewModel vm)
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
