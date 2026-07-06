using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

[QueryProperty(nameof(Mode), "mode")]
public partial class AuthPage : ContentPage
{
    private readonly AuthViewModel _vm;

    public string? Mode
    {
        set { if (!string.IsNullOrEmpty(value)) _vm.Mode = value; }
    }

    public AuthPage(AuthViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}
