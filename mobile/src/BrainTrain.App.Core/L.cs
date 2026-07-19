namespace BrainTrain.App.Core;

/// <summary>
/// Localización ligera ES/EN/PT por diccionario (sin toolchain resx).
/// Las páginas usan {x:Static core:L.Clave}: el idioma se aplica a cada página
/// nueva; tras cambiarlo en Ajustes, un reinicio lo aplica en todas partes.
/// El contenido del juego (preguntas) permanece en español por ahora.
/// </summary>
public static class L
{
    public const string PrefKey = "app.lang";
    public static string Language { get; private set; } = "es";

    public static readonly (string Code, string Name)[] Languages =
        [("es", "Español"), ("en", "English"), ("pt", "Português")];

    public static void SetLanguage(string lang) =>
        Language = lang is "en" or "pt" ? lang : "es";

    private static string T(string es, string en, string pt) =>
        Language switch { "en" => en, "pt" => pt, _ => es };

    // ---------- Onboarding ----------
    public static string App_Slogan => T(
        "Acertijos, lógica y preguntas capciosas.\nEntrena tu ingenio jugando.",
        "Riddles, logic and trick questions.\nTrain your wit by playing.",
        "Enigmas, lógica e perguntas capciosas.\nTreine sua mente jogando.");
    public static string Onboarding_NameQuestion => T("¿Cómo te llamamos?", "What should we call you?", "Como podemos te chamar?");
    public static string Onboarding_NamePlaceholder => T("Tu nombre o apodo (opcional)", "Your name or nickname (optional)", "Seu nome ou apelido (opcional)");
    public static string Onboarding_PickAvatar => T("Elige tu avatar", "Pick your avatar", "Escolha seu avatar");
    public static string Onboarding_PlayNow => T("¡Jugar ya!", "Play now!", "Jogar agora!");
    public static string Onboarding_HaveAccount => T("Ya tengo cuenta", "I have an account", "Já tenho conta");

    // ---------- Home ----------
    public static string Home_DailyTitle => T("⚡ Reto del día", "⚡ Daily challenge", "⚡ Desafio do dia");
    public static string Home_DailyDesc => T(
        "7 preguntas, gratis (no gasta vidas) y con bonus de XP. ¡Mantén tu racha!",
        "7 questions, free (no lives spent) with bonus XP. Keep your streak!",
        "7 perguntas, grátis (não gasta vidas) e com bônus de XP. Mantenha sua sequência!");
    public static string Home_QuickGame => T("🎲 Partida rápida", "🎲 Quick game", "🎲 Partida rápida");
    public static string Home_Training => T("🧠 Entrenamiento", "🧠 Training", "🧠 Treino");
    public static string Home_Focus => T("🧘 Enfoque", "🧘 Focus", "🧘 Foco");
    public static string Home_Duels => T("⚔️ Duelos 1v1", "⚔️ 1v1 Duels", "⚔️ Duelos 1v1");
    public static string Home_Categories => T("Categorías", "Categories", "Categorias");
    public static string Home_QuestsTitle => T("🎁 Misiones de hoy", "🎁 Today's quests", "🎁 Missões de hoje");
    public static string Home_CalibTitle => T("🧪 Descubre tu nivel", "🧪 Find your level", "🧪 Descubra seu nível");
    public static string Home_CalibDesc => T(
        "10 preguntas rápidas (gratis) para calibrar tu dificultad y armar tu radar de habilidades.",
        "10 quick questions (free) to calibrate your difficulty and build your skills radar.",
        "10 perguntas rápidas (grátis) para calibrar sua dificuldade e montar seu radar de habilidades.");
    public static string Home_CalibButton => T("Hacer mi test", "Take my test", "Fazer meu teste");
    public static string Daily_Play => T("Jugar el reto", "Play the challenge", "Jogar o desafio");
    public static string Daily_Done => T("✅ Completado por hoy", "✅ Done for today", "✅ Concluído por hoje");

    // ---------- Quiz / Resultados ----------
    public static string Quiz_Confirm => T("Confirmar respuesta", "Confirm answer", "Confirmar resposta");
    public static string Results_Learn => T("Aprende de cada pregunta", "Learn from every question", "Aprenda com cada pergunta");
    public static string Results_Share => T("📤 Compartir mi resultado", "📤 Share my result", "📤 Compartilhar meu resultado");
    public static string Results_KeepPlaying => T("Seguir jugando", "Keep playing", "Continuar jogando");

    // ---------- Tienda ----------
    public static string Store_Title => T("🛒 Tienda", "🛒 Store", "🛒 Loja");
    public static string Store_FreeLife => T("📺 Vida gratis", "📺 Free life", "📺 Vida grátis");
    public static string Store_FreeLifeDesc => T(
        "Mira un anuncio corto y recibe ❤️ +1 (hasta 5 al día).",
        "Watch a short ad and get ❤️ +1 (up to 5 a day).",
        "Assista a um anúncio curto e ganhe ❤️ +1 (até 5 por dia).");
    public static string Store_WatchAd => T("Ver anuncio", "Watch ad", "Ver anúncio");
    public static string Store_RefillTitle => T("Recarga con monedas", "Refill with coins", "Recarregue com moedas");
    public static string Store_Refill => T("Recargar vidas", "Refill lives", "Recarregar vidas");

    // ---------- Práctica offline ----------
    public static string Practice_Title => T("📴 Práctica", "📴 Practice", "📴 Prática");
    public static string Practice_Desc => T(
        "Sin internet, sin vidas y sin XP: solo tú, las preguntas y sus explicaciones. Feedback al instante.",
        "No internet, no lives, no XP: just you, the questions and their explanations. Instant feedback.",
        "Sem internet, sem vidas e sem XP: só você, as perguntas e suas explicações. Feedback na hora.");
    public static string Practice_Offline => T("Modo sin conexión (pack guardado)", "Offline mode (cached pack)", "Modo offline (pacote salvo)");
    public static string Practice_Next => T("Siguiente ➡️", "Next ➡️", "Próxima ➡️");
    public static string Practice_Correct => T("¡Correcto! 🎉", "Correct! 🎉", "Correto! 🎉");
    public static string Practice_Wrong => T("Casi… mira por qué 👇", "Almost… here's why 👇", "Quase… veja o porquê 👇");
    public static string Home_Practice => T("📴 Práctica (sin conexión)", "📴 Practice (offline)", "📴 Prática (offline)");

    // ---------- Ajustes ----------
    public static string Settings_Title => T("⚙️ Ajustes", "⚙️ Settings", "⚙️ Configurações");
    public static string Settings_Reminder => T("Recordatorio del reto diario", "Daily challenge reminder", "Lembrete do desafio diário");
    public static string Settings_ReminderDesc => T(
        "Una notificación al día para no perder tu racha.",
        "One notification a day so you don't lose your streak.",
        "Uma notificação por dia para não perder sua sequência.");
    public static string Settings_Hour => T("Hora", "Time", "Hora");
    public static string Settings_SaveReminder => T("Guardar recordatorio", "Save reminder", "Salvar lembrete");
    public static string Settings_Account => T("Cuenta", "Account", "Conta");
    public static string Settings_SaveProgress => T("Guardar mi progreso (crear cuenta)", "Save my progress (create account)", "Salvar meu progresso (criar conta)");
    public static string Settings_Logout => T("Cerrar sesión", "Log out", "Sair");
    public static string Settings_Language => T("Idioma", "Language", "Idioma");
    public static string Settings_LanguageNote => T(
        "El cambio completo se aplica al reiniciar la app. El contenido del juego está en español.",
        "Full change applies after restarting the app. Game content is in Spanish.",
        "A mudança completa é aplicada ao reiniciar o app. O conteúdo do jogo está em espanhol.");
}
