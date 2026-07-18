namespace BrainTrain.Api;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>Clave HS256. En producción SIEMPRE vía variable de entorno Jwt__Key.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "braintrain";
    public string Audience { get; set; } = "braintrain-app";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 60;
}

public sealed class GameOptions
{
    public const string Section = "Game";

    public int QuestionsPerSession { get; set; } = 7;
    public int MaxLives { get; set; } = 5;
    public int LifeRegenMinutes { get; set; } = 30;
    public int RefillCoinCost { get; set; } = 100;

    /// <summary>Beneficios Premium: solo conveniencia (más vidas, regeneración rápida) — nunca ventaja competitiva.</summary>
    public int PremiumMaxLives { get; set; } = 8;
    public int PremiumLifeRegenMinutes { get; set; } = 20;

    /// <summary>Vidas máximas por día canjeables viendo anuncios recompensados.</summary>
    public int AdRewardLivesPerDay { get; set; } = 5;

    /// <summary>Tope diario de XP obtenible por minijuegos (anti-farmeo del leaderboard).</summary>
    public int MinigameDailyXpCap { get; set; } = 300;

    /// <summary>XP por respuesta correcta = XpPerDifficulty * dificultad(1..5).</summary>
    public int XpPerDifficulty { get; set; } = 10;
    public int PerfectBonusXp { get; set; } = 25;
    public int PerfectBonusCoins { get; set; } = 5;
    public int SessionCompleteCoins { get; set; } = 2;
    public int DailyBonusXp { get; set; } = 50;
    public int DailyBonusCoins { get; set; } = 10;
    public int LevelUpCoins { get; set; } = 20;
    public int DailyQuestions { get; set; } = 7;
    public int CatalogCacheMinutes { get; set; } = 15;

    /// <summary>Edad mínima de la sesión antes de aceptar un submit (anti-bot básico).</summary>
    public int MinSessionSeconds { get; set; } = 4;

    /// <summary>Tiempo máximo de vida de una sesión abierta.</summary>
    public int SessionExpiryMinutes { get; set; } = 60;
}

public sealed class StoreOptions
{
    public const string Section = "Store";

    /// <summary>Solo para desarrollo/pruebas: acepta recibos con plataforma "Test".</summary>
    public bool AllowTestReceipts { get; set; }

    public List<StoreProduct> Products { get; set; } = [];
}

public sealed class StoreProduct
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Lives { get; set; }
    public int Coins { get; set; }

    /// <summary>Días de membresía Premium que otorga (0 = no es membresía).</summary>
    public int PremiumDays { get; set; }

    /// <summary>Precio orientativo para UI; el precio real lo fija la tienda (Play/App Store).</summary>
    public string PriceHint { get; set; } = string.Empty;

    /// <summary>Precio cobrado vía PayPal en el portal web (USD).</summary>
    public decimal PriceUsd { get; set; }
}

public sealed class PayPalOptions
{
    public const string Section = "PayPal";

    /// <summary>Credenciales REST de PayPal. En producción SIEMPRE por variables de entorno.</summary>
    public string ClientId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;

    /// <summary>"sandbox" o "live".</summary>
    public string Mode { get; set; } = "sandbox";
    public string Currency { get; set; } = "USD";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(Secret);
    public string BaseUrl => Mode.Equals("live", StringComparison.OrdinalIgnoreCase)
        ? "https://api-m.paypal.com"
        : "https://api-m.sandbox.paypal.com";
}
