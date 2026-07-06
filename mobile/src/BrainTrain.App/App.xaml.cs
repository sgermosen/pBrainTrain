using BrainTrain.App.Core;

namespace BrainTrain.App;

public partial class App : Application
{
    private readonly ApiClient _api;

    public App(ApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell(_api));
    }
}
