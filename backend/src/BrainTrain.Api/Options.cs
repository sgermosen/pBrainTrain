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

    /// <summary>Precio orientativo para UI; el precio real lo fija la tienda (Play/App Store).</summary>
    public string PriceHint { get; set; } = string.Empty;
}
