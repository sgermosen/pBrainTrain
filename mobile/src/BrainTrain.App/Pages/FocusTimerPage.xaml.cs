using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class FocusTimerPage : ContentPage
{
    private readonly FocusTimerViewModel _vm;

    public FocusTimerPage(FocusTimerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Dispose();
    }

    // Durante un bloque de foco el botón atrás no interrumpe (usa ✕ para cancelar).
    protected override bool OnBackButtonPressed() => _vm.IsRunning;
}
