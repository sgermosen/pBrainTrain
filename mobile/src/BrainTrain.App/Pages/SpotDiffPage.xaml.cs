using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Pages;

public partial class SpotDiffPage : ContentPage
{
    private readonly SpotDiffViewModel _vm;

    public SpotDiffPage(SpotDiffViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        // Los toques llegan con posición en píxeles del layout → se normalizan 0..1.
        AddTap(TopLayout);
        AddTap(BottomLayout);
    }

    private void AddTap(AbsoluteLayout layout)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, e) =>
        {
            var p = e.GetPosition(layout);
            if (p is null || layout.Width <= 0 || layout.Height <= 0) return;
            await _vm.TapAsync(p.Value.X / layout.Width, p.Value.Y / layout.Height);
        };
        layout.GestureRecognizers.Add(tap);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Start();
    }
}
