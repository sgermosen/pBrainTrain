namespace BrainTrain.App.Core.Focus;

/// <summary>Fase de un ciclo de respiración. Motion: 1=inhalar (pacer crece), -1=exhalar, 0=sostener.</summary>
public sealed record BreathPhase(string Label, double Seconds, int Motion);

/// <summary>Instrucción con tiempo para sesiones guiadas por texto (NSDR).</summary>
public sealed record TimedPrompt(string Text, double Seconds);

public sealed record BreathProtocol(
    string Code, string Name, string Emoji, string Description,
    IReadOnlyList<BreathPhase> Cycle, int DefaultMinutes,
    string EvidenceNote, IReadOnlyList<TimedPrompt>? Prompts = null)
{
    public double CycleSeconds => Cycle.Sum(p => p.Seconds);
}

/// <summary>
/// Motor puro de respiración guiada: dado el tiempo transcurrido devuelve la
/// fase actual y el progreso dentro de ella (para animar el pacer).
/// </summary>
public sealed class BreathingEngine(BreathProtocol protocol, int minutes)
{
    public BreathProtocol Protocol { get; } = protocol;
    public double TotalSeconds { get; } = minutes * 60;

    public bool IsComplete(double elapsed) => elapsed >= TotalSeconds;

    public (BreathPhase Phase, double Fraction, int Cycle) PhaseAt(double elapsed)
    {
        var cycleLen = Protocol.CycleSeconds;
        var cycleIndex = (int)(elapsed / cycleLen);
        var inCycle = elapsed % cycleLen;

        foreach (var phase in Protocol.Cycle)
        {
            if (inCycle < phase.Seconds)
                return (phase, inCycle / phase.Seconds, cycleIndex);
            inCycle -= phase.Seconds;
        }
        var last = Protocol.Cycle[^1];
        return (last, 1, cycleIndex);
    }

    /// <summary>Escala del pacer (0.35–1.0) para la fase y fracción dadas.</summary>
    public static double PacerScale(BreathPhase phase, double fraction, double previousScale) => phase.Motion switch
    {
        1 => 0.35 + 0.65 * fraction,
        -1 => 1.0 - 0.65 * fraction,
        _ => previousScale
    };

    public TimedPrompt? PromptAt(double elapsed)
    {
        if (Protocol.Prompts is null) return null;
        var t = elapsed;
        foreach (var p in Protocol.Prompts)
        {
            if (t < p.Seconds) return p;
            t -= p.Seconds;
        }
        return Protocol.Prompts[^1];
    }
}

/// <summary>
/// Protocolos incluidos. Cada uno declara su nota de evidencia — la app no
/// promete milagros: dice qué se sabe y de dónde sale.
/// </summary>
public static class BreathProtocols
{
    public static readonly BreathProtocol CyclicSigh = new(
        "cyclic_sigh", "Suspiro fisiológico", "🌬️",
        "Doble inhalación por la nariz + exhalación larga por la boca. La técnica con mejor evidencia para bajar el estrés rápido.",
        [
            new BreathPhase("Inhala por la nariz", 2.0, 1),
            new BreathPhase("Otra inhalación corta", 1.0, 1),
            new BreathPhase("Exhala lento por la boca", 6.0, -1)
        ],
        DefaultMinutes: 5,
        EvidenceNote: "5 min/día de 'cyclic sighing' mejoró el ánimo y redujo la frecuencia respiratoria más que mindfulness en un ensayo controlado (Balban et al., 2023, Cell Reports Medicine).");

    public static readonly BreathProtocol Box = new(
        "box", "Respiración en caja", "📦",
        "4-4-4-4: inhala, sostén, exhala, sostén. Usada para mantener la calma bajo presión.",
        [
            new BreathPhase("Inhala", 4.0, 1),
            new BreathPhase("Sostén", 4.0, 0),
            new BreathPhase("Exhala", 4.0, -1),
            new BreathPhase("Sostén", 4.0, 0)
        ],
        DefaultMinutes: 4,
        EvidenceNote: "La respiración lenta y controlada activa la respuesta parasimpática (revisiones sistemáticas de slow-paced breathing). El patrón exacto importa menos que la lentitud y la constancia.");

    public static readonly BreathProtocol LongExhale = new(
        "long_exhale", "Exhalación larga", "🍃",
        "Inhala 4, exhala 8. Alargar la exhalación es la palanca fisiológica más directa para desacelerar el corazón.",
        [
            new BreathPhase("Inhala", 4.0, 1),
            new BreathPhase("Exhala muy lento", 8.0, -1)
        ],
        DefaultMinutes: 5,
        EvidenceNote: "Exhalaciones más largas que las inhalaciones aumentan la variabilidad cardiaca y la activación vagal (literatura de respiración lenta ~6 respiraciones/min).");

    public static readonly BreathProtocol Nsdr = new(
        "nsdr", "Reinicio mental (NSDR)", "🛌",
        "Descanso profundo sin dormir: 10 minutos de escaneo corporal guiado para despejar la mente.",
        [ new BreathPhase("Respira natural", 10.0, 1), new BreathPhase("Suelta el aire", 10.0, -1) ],
        DefaultMinutes: 10,
        EvidenceNote: "El Yoga Nidra/NSDR reduce estrés y ansiedad en estudios controlados pequeños; un estudio PET clásico observó cambios en dopamina estriatal durante la práctica (Kjaer et al., 2002).",
        Prompts:
        [
            new TimedPrompt("Acomódate. Puedes cerrar los ojos o dejar la mirada suave.", 25),
            new TimedPrompt("Haz dos suspiros: doble inhalación por la nariz y exhalación larga por la boca.", 30),
            new TimedPrompt("Deja que la respiración vuelva a su ritmo natural. No la controles.", 35),
            new TimedPrompt("Lleva la atención a tus pies. Nota su peso, su temperatura.", 45),
            new TimedPrompt("Sube a las pantorrillas y rodillas. Suéltalas.", 40),
            new TimedPrompt("Nota tus muslos y caderas hundiéndose en el asiento.", 40),
            new TimedPrompt("Afloja el abdomen. Deja que la respiración lo mueva sola.", 45),
            new TimedPrompt("Suelta los hombros. Deja que caigan lejos de las orejas.", 45),
            new TimedPrompt("Relaja los brazos hasta la punta de los dedos.", 40),
            new TimedPrompt("Afloja la mandíbula, la lengua, el entrecejo.", 45),
            new TimedPrompt("Siente todo el cuerpo a la vez, pesado y quieto.", 50),
            new TimedPrompt("Si aparece un pensamiento, obsérvalo pasar como una nube. Vuelve al cuerpo.", 60),
            new TimedPrompt("Quédate aquí. No hay nada que hacer ni resolver.", 60),
            new TimedPrompt("Empieza a mover suavemente dedos de manos y pies.", 40),
            new TimedPrompt("Respira profundo una vez y abre los ojos. Listo: mente en blanco.", 40)
        ]);

    public static readonly IReadOnlyList<BreathProtocol> All = [CyclicSigh, Box, LongExhale, Nsdr];
}

/// <summary>Sonidos de fondo de la sección Enfoque (assets locales, loops sin costuras).</summary>
public sealed record FocusSound(string Code, string Name, string Emoji, string? Asset, string Note);

public static class FocusSounds
{
    public static readonly IReadOnlyList<FocusSound> All =
    [
        new("silence", "Silencio", "🔇", null, "Para muchas personas es la mejor opción."),
        new("brown", "Ruido marrón", "🟤", "focus/noise_brown.wav", "Popular para enmascarar ruido ambiente. Sin estudios propios: pruébalo y decide."),
        new("pink", "Ruido rosa", "🌸", "focus/noise_pink.wav", "Ayuda a algunas personas (evidencia en TDAH); a otras les estorba."),
        new("rain", "Lluvia", "🌧️", "focus/rain.wav", "Sonido de naturaleza: reduce estrés (PNAS 2021)."),
        new("binaural40", "Binaural 40 Hz", "🎧", "focus/binaural_focus40.wav", "Requiere audífonos. Evidencia mixta: pruébalo y decide."),
        new("binaural6", "Binaural 6 Hz (calma)", "🎧", "focus/binaural_calm6.wav", "Requiere audífonos. Evidencia mixta.")
    ];
}
