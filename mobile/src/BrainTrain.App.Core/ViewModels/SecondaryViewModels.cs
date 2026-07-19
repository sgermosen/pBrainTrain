using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainTrain.App.Core.ViewModels;

// ---------------------------------------------------------------- Perfil
public partial class ProfileViewModel(ApiClient api, INavigationService nav) : ObservableObject
{
    [ObservableProperty] private ProfileDto? _profile;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _message;

    public ObservableCollection<SkillDto> Skills { get; } = [];
    public ObservableCollection<AvatarShopItemDto> AvatarShop { get; } = [];

    public double LevelProgress => Profile is null || Profile.XpForNextLevel == 0
        ? 0 : Math.Clamp((double)Profile.XpIntoLevel / Profile.XpForNextLevel, 0, 1);
    public string Accuracy => Profile is null || Profile.Totals.Answered == 0
        ? "—" : $"{100.0 * Profile.Totals.Correct / Profile.Totals.Answered:F0}%";

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        Error = null;
        try
        {
            Profile = await api.GetProfileAsync();
            var skills = await api.GetSkillsAsync();
            Skills.Clear();
            foreach (var s in skills.Skills) Skills.Add(s);
            AvatarShop.Clear();
            foreach (var a in await api.GetAvatarShopAsync())
                AvatarShop.Add(a);
            OnPropertyChanged(nameof(LevelProgress));
            OnPropertyChanged(nameof(Accuracy));
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }

    /// <summary>Compra un avatar premium con monedas (queda equipado al instante).</summary>
    [RelayCommand]
    private async Task BuyAvatarAsync(AvatarShopItemDto item)
    {
        if (item.Owned) { await ChangeAvatarAsync(item.Code); return; }
        try
        {
            Profile = await api.BuyAvatarAsync(item.Code);
            Message = $"🎉 ¡Avatar nuevo! Te quedan {Profile.Coins} 🪙";
            var i = AvatarShop.IndexOf(item);
            if (i >= 0) AvatarShop[i] = item with { Owned = true };
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
    }

    [RelayCommand]
    private async Task ChangeAvatarAsync(string avatarCode)
    {
        try { Profile = await api.UpdateProfileAsync(new UpdateProfileRequest(null, avatarCode)); }
        catch (ApiException e) { Error = e.Message; }
    }

    [RelayCommand] private Task GoSettingsAsync() => nav.GoToAsync("settings");
}

// ---------------------------------------------------------------- Logros
public partial class AchievementsViewModel(ApiClient api) : ObservableObject
{
    public ObservableCollection<AchievementDto> Achievements { get; } = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private int _unlockedCount;
    [ObservableProperty] private int _totalCount;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        Error = null;
        try
        {
            var list = await api.GetAchievementsAsync();
            Achievements.Clear();
            // Desbloqueados primero, luego por cercanía a completarse (motiva el "casi lo tengo").
            foreach (var a in list
                         .OrderByDescending(a => a.Unlocked)
                         .ThenByDescending(a => a.Threshold == 0 ? 0 : (double)a.Progress / a.Threshold))
                Achievements.Add(a);
            UnlockedCount = list.Count(a => a.Unlocked);
            TotalCount = list.Count;
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }
}

// ---------------------------------------------------------------- Leaderboard
public partial class LeaderboardViewModel(ApiClient api) : ObservableObject
{
    public ObservableCollection<LeaderboardEntryDto> Top { get; } = [];
    [ObservableProperty] private int? _myRank;
    [ObservableProperty] private int _myWeeklyXp;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        Error = null;
        try
        {
            var board = await api.GetLeaderboardAsync();
            Top.Clear();
            foreach (var e in board.Top) Top.Add(e);
            MyRank = board.MyRank;
            MyWeeklyXp = board.MyWeeklyXp;
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }
}

// ---------------------------------------------------------------- Tienda
public partial class StoreViewModel(
    ApiClient api, IPlatformPurchaser purchaser, IAdService ads) : ObservableObject
{
    [ObservableProperty] private int? _adLivesRemaining;

    /// <summary>Ver un anuncio recompensado a cambio de una vida (tope diario en el servidor).</summary>
    [RelayCommand]
    private async Task WatchAdForLifeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        Message = null;
        try
        {
            if (!await ads.ShowRewardedAdAsync())
                return; // el usuario cerró el anuncio antes de terminar

            var reward = await api.ClaimAdRewardAsync();
            Profile = reward.Profile;
            AdLivesRemaining = reward.RemainingToday;
            Message = $"❤️ ¡+1 vida! Te quedan {reward.RemainingToday} anuncios hoy.";
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }

    public ObservableCollection<StoreProductDto> Products { get; } = [];
    [ObservableProperty] private ProfileDto? _profile;
    [ObservableProperty] private int _refillCoinCost;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _message;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        Error = null;
        try
        {
            var catalog = await api.GetStoreCatalogAsync();
            Products.Clear();
            foreach (var p in catalog.Products) Products.Add(p);
            RefillCoinCost = catalog.RefillCoinCost;
            Profile = await api.GetProfileAsync();
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task BuyAsync(StoreProductDto product)
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        Message = null;
        try
        {
            // 1) Compra nativa en la tienda de la plataforma.
            var purchase = await purchaser.BuyAsync(product.ProductId);
            if (purchase is null) return; // usuario canceló

            // 2) Canje verificado en el servidor (anti-fraude).
            var result = await api.PurchaseAsync(new PurchaseRequest(
                purchase.Platform, product.ProductId, purchase.TransactionId, purchase.Receipt));
            Profile = result.Profile;
            Message = result.LivesGranted > 0
                ? $"⚡ +{result.LivesGranted} vidas. ¡A jugar!"
                : $"🪙 +{result.CoinsGranted} monedas.";
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RefillWithCoinsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        Message = null;
        try
        {
            Profile = await api.RefillWithCoinsAsync();
            Message = "⚡ ¡Vidas recargadas con tus monedas!";
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
        finally { IsBusy = false; }
    }
}

// ---------------------------------------------------------------- Ajustes
public partial class SettingsViewModel(
    ApiClient api, IReminderScheduler reminders, IAppPreferences prefs, INavigationService nav) : ObservableObject
{
    public const string ReminderEnabledKey = "reminder.enabled";
    public const string ReminderHourKey = "reminder.hour";

    [ObservableProperty] private bool _remindersEnabled;
    [ObservableProperty] private TimeSpan _reminderTime = new(19, 0, 0);
    [ObservableProperty] private ProfileDto? _profile;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _message;

    [RelayCommand]
    public async Task LoadAsync()
    {
        RemindersEnabled = prefs.Get(ReminderEnabledKey) == "1";
        if (TimeSpan.TryParse(prefs.Get(ReminderHourKey), out var t))
            ReminderTime = t;
        var lang = prefs.Get(L.PrefKey, "es");
        LanguageIndex = Array.FindIndex(L.Languages, l => l.Code == lang) is var i and >= 0 ? i : 0;
        try { Profile = await api.GetProfileAsync(); }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión."; }
    }

    [RelayCommand]
    public async Task ApplyRemindersAsync()
    {
        Error = null;
        if (RemindersEnabled)
        {
            if (!await reminders.RequestPermissionAsync())
            {
                RemindersEnabled = false;
                Error = "Activa el permiso de notificaciones para recibir recordatorios.";
                return;
            }
            await reminders.ScheduleDailyAsync(ReminderTime,
                "🧠 Tu reto diario te espera",
                "5 minutos de ingenio mantienen tu racha viva. ¡Hoy también puedes!");
            Message = $"Recordatorio diario a las {ReminderTime:hh\\:mm}.";
        }
        else
        {
            await reminders.CancelAllAsync();
            Message = "Recordatorios desactivados.";
        }
        prefs.Set(ReminderEnabledKey, RemindersEnabled ? "1" : "0");
        prefs.Set(ReminderHourKey, ReminderTime.ToString());
    }

    // ----- Idioma (ES/EN/PT) -----
    public IReadOnlyList<string> LanguageNames { get; } = L.Languages.Select(l => l.Name).ToList();

    [ObservableProperty] private int _languageIndex;

    /// <summary>El cambio del Picker aplica el idioma al instante (páginas nuevas).</summary>
    partial void OnLanguageIndexChanged(int value)
    {
        var code = L.Languages[Math.Clamp(value, 0, L.Languages.Length - 1)].Code;
        if (prefs.Get(L.PrefKey, "es") == code) return;
        prefs.Set(L.PrefKey, code);
        L.SetLanguage(code);
        Message = L.Settings_LanguageNote;
    }

    [RelayCommand]
    private Task GoUpgradeAsync() => nav.GoToAsync("auth?mode=upgrade");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await api.LogoutAsync();
        await nav.GoToAsync("//onboarding");
    }
}

// ---------------------------------------------------------------- Auth (login/registro/ascenso)
public partial class AuthViewModel(ApiClient api, INavigationService nav) : ObservableObject
{
    /// <summary>"login", "register" o "upgrade" (invitado → cuenta completa).</summary>
    [ObservableProperty] private string _mode = "login";
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;

    public bool IsLogin => Mode == "login";
    public bool IsRegister => Mode == "register";
    public bool IsUpgrade => Mode == "upgrade";
    public string Title => Mode switch
    {
        "register" => "Crear cuenta",
        "upgrade" => "Guarda tu progreso",
        _ => "Iniciar sesión"
    };

    partial void OnModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsLogin));
        OnPropertyChanged(nameof(IsRegister));
        OnPropertyChanged(nameof(IsUpgrade));
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    private void SwitchMode(string mode) => Mode = mode;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        try
        {
            _ = Mode switch
            {
                "register" => await api.RegisterAsync(Email.Trim(), Password, DisplayName.Trim()),
                "upgrade" => await api.UpgradeAsync(Email.Trim(), Password),
                _ => await api.LoginAsync(Email.Trim(), Password)
            };
            await nav.GoToAsync("//home");
        }
        catch (ApiException e) { Error = e.Message; }
        catch (HttpRequestException) { Error = "Sin conexión. Revisa tu internet."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task BackAsync() => nav.GoBackAsync();
}
