using BrainTrain.App.Core;
using BrainTrain.App.Core.Focus;
using BrainTrain.App.Core.ViewModels;

namespace BrainTrain.App.Core.Tests;

public class BreathingEngineTests
{
    [Fact]
    public void CyclicSigh_RecorreLasFasesEnOrden()
    {
        var engine = new BreathingEngine(BreathProtocols.CyclicSigh, 5);
        Assert.Equal(9.0, BreathProtocols.CyclicSigh.CycleSeconds);

        Assert.Equal("Inhala por la nariz", engine.PhaseAt(0.5).Phase.Label);
        Assert.Equal("Otra inhalación corta", engine.PhaseAt(2.5).Phase.Label);
        Assert.Equal("Exhala lento por la boca", engine.PhaseAt(5.0).Phase.Label);
        // Segundo ciclo: vuelve al inicio.
        var (phase, _, cycle) = engine.PhaseAt(9.5);
        Assert.Equal("Inhala por la nariz", phase.Label);
        Assert.Equal(1, cycle);
    }

    [Fact]
    public void PhaseAt_DevuelveFraccionCorrecta()
    {
        var engine = new BreathingEngine(BreathProtocols.Box, 4);
        var (phase, fraction, _) = engine.PhaseAt(2.0); // mitad del "Inhala" de 4 s
        Assert.Equal("Inhala", phase.Label);
        Assert.Equal(0.5, fraction, 3);
    }

    [Fact]
    public void PacerScale_CreceInhalando_BajaExhalando_SeMantieneSosteniendo()
    {
        var inhale = new BreathPhase("in", 4, 1);
        var exhale = new BreathPhase("out", 4, -1);
        var hold = new BreathPhase("hold", 4, 0);

        Assert.True(BreathingEngine.PacerScale(inhale, 1.0, 0.5) > BreathingEngine.PacerScale(inhale, 0.0, 0.5));
        Assert.True(BreathingEngine.PacerScale(exhale, 1.0, 0.5) < BreathingEngine.PacerScale(exhale, 0.0, 0.5));
        Assert.Equal(0.77, BreathingEngine.PacerScale(hold, 0.5, 0.77));
    }

    [Fact]
    public void Nsdr_TienePromptsQueCubrenLosDiezMinutos()
    {
        var nsdr = BreathProtocols.Nsdr;
        Assert.NotNull(nsdr.Prompts);
        var total = nsdr.Prompts!.Sum(p => p.Seconds);
        Assert.True(total >= nsdr.DefaultMinutes * 60,
            $"los prompts cubren {total}s pero la sesión dura {nsdr.DefaultMinutes * 60}s");

        var engine = new BreathingEngine(nsdr, nsdr.DefaultMinutes);
        Assert.Equal(nsdr.Prompts[0].Text, engine.PromptAt(1)?.Text);
        Assert.NotNull(engine.PromptAt(9 * 60));
    }

    [Fact]
    public void TodosLosProtocolosDeclaranSuEvidencia()
    {
        Assert.All(BreathProtocols.All, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.EvidenceNote));
            Assert.True(p.CycleSeconds > 0);
            Assert.True(p.DefaultMinutes >= 3); // el backend exige 3 min para contar
        });
    }

    [Fact]
    public void LosSonidosDeclaranNotaHonesta()
    {
        Assert.All(FocusSounds.All, s => Assert.False(string.IsNullOrWhiteSpace(s.Note)));
        Assert.Contains(FocusSounds.All, s => s.Asset is null); // opción silencio
    }
}

public class FocusE2ETests : IClassFixture<BackendFixture>
{
    private readonly BackendFixture _backend;

    public FocusE2ETests(BackendFixture backend) => _backend = backend;

    private async Task<ApiClient> LoggedGuestAsync()
    {
        var api = _backend.NewApiClient(out _);
        var nav = new FakeNav();
        await new OnboardingViewModel(api, new FakeDevice(), nav).PlayAsGuestCommand.ExecuteAsync(null);
        return api;
    }

    [Fact]
    public async Task SesionDeEnfoque_OtorgaXpYRacha_ConTopeDiario()
    {
        var api = await LoggedGuestAsync();

        // Sesión válida de 5 minutos.
        var r1 = await api.CompleteFocusAsync("work", 300);
        Assert.Equal(10, r1.XpEarned);
        Assert.Equal(1, r1.Streak.Current);
        Assert.Equal(20, r1.DailyXpRemaining);

        // El tope diario es 30 XP: a la cuarta sesión ya no da XP.
        await api.CompleteFocusAsync("calm", 300);
        var r3 = await api.CompleteFocusAsync("reset", 600);
        Assert.Equal(10, r3.XpEarned);
        Assert.Equal(0, r3.DailyXpRemaining);
        var r4 = await api.CompleteFocusAsync("work", 300);
        Assert.Equal(0, r4.XpEarned);
    }

    [Fact]
    public async Task SesionDemasiadoCorta_NoCuenta()
    {
        var api = await LoggedGuestAsync();
        var e = await Assert.ThrowsAsync<ApiException>(() => api.CompleteFocusAsync("work", 60));
        Assert.Equal(422, e.Status);
    }
}
