using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class FocusPage : ContentPage
{
    public FocusPage(FocusHubViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
