namespace BrainTrain.Domain;

public class Achievement
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🏆";
    public AchievementTier Tier { get; set; }
    public int XpReward { get; set; }
    public int CoinReward { get; set; }
    public AchievementCriteria CriteriaType { get; set; }
    public int Threshold { get; set; }

    /// <summary>Solo para criterios por categoría (maestría).</summary>
    public int? CategoryId { get; set; }
}

public class UserAchievement
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }
    public DateTime UnlockedAtUtc { get; set; }
}

/// <summary>
/// Recibo de compra validado. TransactionId único evita re-canjear el mismo
/// recibo (replay). El recibo crudo no se almacena, solo su hash.
/// </summary>
public class PurchaseReceipt
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }
    public StorePlatform Platform { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string ReceiptHash { get; set; } = string.Empty;
    public int LivesGranted { get; set; }
    public int CoinsGranted { get; set; }
    public DateTime PurchasedAtUtc { get; set; }
}
