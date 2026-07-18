using BrainTrain.App.Core;
using BrainTrain.App.Pages;

namespace BrainTrain.App;

public partial class AppShell : Shell
{
    public AppShell(ApiClient api)
    {
        InitializeComponent();

        Routing.RegisterRoute("auth", typeof(AuthPage));
        Routing.RegisterRoute("quiz", typeof(QuizPage));
        Routing.RegisterRoute("results", typeof(ResultsPage));
        Routing.RegisterRoute("profile", typeof(ProfilePage));
        Routing.RegisterRoute("achievements", typeof(AchievementsPage));
        Routing.RegisterRoute("leaderboard", typeof(LeaderboardPage));
        Routing.RegisterRoute("store", typeof(StorePage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("training", typeof(TrainingPage));
        Routing.RegisterRoute("game2048", typeof(Game2048Page));
        Routing.RegisterRoute("mathsprint", typeof(MathSprintPage));
        Routing.RegisterRoute("wordsearch", typeof(WordSearchPage));
        Routing.RegisterRoute("memorypairs", typeof(MemoryPairsPage));
        Routing.RegisterRoute("simon", typeof(SimonPage));
        Routing.RegisterRoute("spotdiff", typeof(SpotDiffPage));
        Routing.RegisterRoute("rubikguide", typeof(RubikGuidePage));
        Routing.RegisterRoute("focus", typeof(FocusPage));
        Routing.RegisterRoute("focustimer", typeof(FocusTimerPage));
        Routing.RegisterRoute("breathe", typeof(BreathingPage));
        Routing.RegisterRoute("focusscience", typeof(FocusSciencePage));

        // Si hay sesión guardada, entra directo al juego.
        Dispatcher.Dispatch(async () =>
        {
            await api.InitializeAsync();
            if (api.HasSession)
                await GoToAsync("//home");
        });
    }
}
