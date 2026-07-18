using System.Security.Cryptography;
using System.Text;
using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

/// <summary>
/// Verificación de recibos por plataforma. Las implementaciones reales llaman
/// a Google Play Developer API / App Store Server API con las credenciales
/// configuradas (ver docs/PUBLICACION.md). Sin credenciales, rechazan siempre:
/// nunca se otorgan compras sin verificar.
/// </summary>
public interface IPurchaseVerifier
{
    Task<bool> VerifyAsync(StorePlatform platform, string productId, string transactionId, string receipt, CancellationToken ct);
}

public sealed class DefaultPurchaseVerifier(IOptions<StoreOptions> options, ILogger<DefaultPurchaseVerifier> logger) : IPurchaseVerifier
{
    public Task<bool> VerifyAsync(StorePlatform platform, string productId, string transactionId, string receipt, CancellationToken ct)
    {
        if (platform == StorePlatform.Test)
            return Task.FromResult(options.Value.AllowTestReceipts && receipt == "TEST-OK");

        // Punto de extensión: integrar Google Play Developer API (purchases.products.get)
        // y App Store Server API (JWS transaction verification). Documentado en PUBLICACION.md.
        logger.LogWarning("Verificación de recibos de {Platform} no configurada; compra rechazada", platform);
        return Task.FromResult(false);
    }
}

public sealed class StoreService(
    AppDbContext db,
    IPurchaseVerifier verifier,
    IOptions<StoreOptions> storeOptions,
    IOptions<GameOptions> gameOptions)
{
    private readonly StoreOptions _store = storeOptions.Value;
    private readonly GameOptions _game = gameOptions.Value;

    public StoreCatalogDto Catalog() => new(
        _store.Products.Select(p => new StoreProductDto(p.ProductId, p.Name, p.Description, p.Lives, p.Coins, p.PriceHint)).ToList(),
        _game.RefillCoinCost);

    public async Task<PurchaseResponse> PurchaseAsync(long userId, PurchaseRequest req, CancellationToken ct)
    {
        var product = _store.Products.FirstOrDefault(p => p.ProductId == req.ProductId)
                      ?? throw new GameError(400, "unknown_product", "Producto desconocido.");

        if (string.IsNullOrWhiteSpace(req.TransactionId) || string.IsNullOrWhiteSpace(req.Receipt))
            throw new GameError(400, "bad_receipt", "Recibo incompleto.");

        var valid = await verifier.VerifyAsync(req.Platform, req.ProductId, req.TransactionId, req.Receipt, ct);
        if (!valid)
            throw new GameError(402, "invalid_receipt", "No se pudo verificar la compra.");

        var duplicate = await db.PurchaseReceipts.AsNoTracking()
            .AnyAsync(r => r.Platform == req.Platform && r.TransactionId == req.TransactionId, ct);
        if (duplicate)
            throw new GameError(409, "duplicate_receipt", "Esta compra ya fue canjeada.");

        return await GrantProductAsync(userId, product, req.Platform, req.TransactionId, req.Receipt, ct);
    }

    /// <summary>
    /// Acredita un producto ya verificado (compra de tienda o captura PayPal)
    /// registrando el recibo con protección anti-replay.
    /// </summary>
    public async Task<PurchaseResponse> GrantProductAsync(
        long userId, StoreProduct product, StorePlatform platform, string transactionId, string receipt, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");

        var now = DateTime.UtcNow;

        // Materializa la regeneración pendiente antes de sumar vidas compradas.
        var (maxLives, regen) = ProgressionLogic.LivesParams(user, _game, now);
        var (lives, anchor, _) = ProgressionLogic.ComputeLives(
            user.Lives, user.LivesUpdatedAtUtc, now, maxLives, regen);
        user.Lives = lives + product.Lives; // las compradas pueden superar el tope
        user.LivesUpdatedAtUtc = user.Lives >= maxLives ? now : anchor;
        user.Coins += product.Coins;
        if (product.Coins > 0) user.CoinsEarnedTotal += product.Coins;

        // Membresía: extiende desde ahora o desde el vencimiento futuro actual.
        if (product.PremiumDays > 0)
        {
            var from = user.PremiumUntilUtc is { } until && until > now ? until : now;
            user.PremiumUntilUtc = from.AddDays(product.PremiumDays);
        }

        db.PurchaseReceipts.Add(new PurchaseReceipt
        {
            UserId = userId,
            Platform = platform,
            ProductId = product.ProductId,
            TransactionId = transactionId,
            ReceiptHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(receipt))),
            LivesGranted = product.Lives,
            CoinsGranted = product.Coins,
            PurchasedAtUtc = now
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Carrera con el índice único (Platform, TransactionId): otro request canjeó primero.
            throw new GameError(409, "duplicate_receipt", "Esta compra ya fue canjeada.");
        }

        return new PurchaseResponse(product.Lives, product.Coins, ProfileMapper.ToDto(user, _game, now));
    }

    public StoreProduct? FindProduct(string productId) =>
        _store.Products.FirstOrDefault(p => p.ProductId == productId);

    /// <summary>Canjea monedas ganadas jugando por una recarga completa de vidas.</summary>
    public async Task<ProfileDto> RefillWithCoinsAsync(long userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");

        var now = DateTime.UtcNow;
        var (maxLives, regen) = ProgressionLogic.LivesParams(user, _game, now);
        var (lives, _, _) = ProgressionLogic.ComputeLives(
            user.Lives, user.LivesUpdatedAtUtc, now, maxLives, regen);

        if (lives >= maxLives)
            throw new GameError(409, "lives_full", "Ya tienes las vidas completas.");
        if (user.Coins < _game.RefillCoinCost)
            throw new GameError(402, "not_enough_coins", $"Necesitas {_game.RefillCoinCost} monedas.");

        user.Coins -= _game.RefillCoinCost;
        user.Lives = maxLives;
        user.LivesUpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);

        return ProfileMapper.ToDto(user, _game, now);
    }
}
