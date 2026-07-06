using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class QuizPage : ContentPage
{
    private readonly QuizViewModel _vm;

    public QuizPage(QuizViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Dispose();
    }

    // Deshabilita el botón "atrás" del sistema durante la partida (evita
    // abandonos accidentales; el ✕ de la esquina sigue disponible).
    protected override bool OnBackButtonPressed() => true;
}
