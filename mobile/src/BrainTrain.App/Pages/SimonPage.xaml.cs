using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class SimonPage : ContentPage
{
    public SimonPage(SimonViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
