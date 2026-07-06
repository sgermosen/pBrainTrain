using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

public sealed class AuthService(
    AppDbContext db,
    TokenService tokens,
    IOptions<GameOptions> gameOptions)
{
    private static readonly PasswordHasher<User> Hasher = new();
    private readonly GameOptions _game = gameOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        ValidateCredentials(req.Email, req.Password);
        var displayName = ValidateDisplayName(req.DisplayName);
        var email = req.Email.Trim().ToLowerInvariant();

        if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct))
            throw new GameError(409, "email_taken", "Ese correo ya está registrado.");

        var user = NewUser(displayName);
        user.Email = email;
        user.PasswordHash = Hasher.HashPassword(user, req.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user?.PasswordHash is null ||
            Hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        {
            throw new GameError(401, "bad_credentials", "Correo o contraseña incorrectos.");
        }
        return await IssueAsync(user, ct);
    }

    /// <summary>
    /// Entrada sin fricción: crea (o recupera) una cuenta de invitado ligada al
    /// dispositivo. Clave para que un niño o un adulto empiecen a jugar en
    /// segundos; la cuenta puede ascender a email+contraseña después.
    /// </summary>
    public async Task<AuthResponse> GuestAsync(GuestRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.DeviceId) || req.DeviceId.Length is < 8 or > 128)
            throw new GameError(400, "bad_device", "Identificador de dispositivo inválido.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.DeviceId == req.DeviceId, ct);
        if (user is null)
        {
            user = NewUser(string.IsNullOrWhiteSpace(req.DisplayName)
                ? $"Jugador{Random.Shared.Next(1000, 9999)}"
                : ValidateDisplayName(req.DisplayName!));
            user.DeviceId = req.DeviceId;
            user.IsGuest = true;
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        return await IssueAsync(user, ct);
    }

    /// <summary>Convierte una cuenta de invitado en cuenta completa conservando todo el progreso.</summary>
    public async Task<AuthResponse> UpgradeAsync(long userId, UpgradeRequest req, CancellationToken ct)
    {
        ValidateCredentials(req.Email, req.Password);
        var email = req.Email.Trim().ToLowerInvariant();

        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");
        if (!user.IsGuest)
            throw new GameError(409, "not_guest", "La cuenta ya está registrada.");
        if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == email && u.Id != userId, ct))
            throw new GameError(409, "email_taken", "Ese correo ya está registrado.");

        user.Email = email;
        user.PasswordHash = Hasher.HashPassword(user, req.Password);
        user.IsGuest = false;
        await db.SaveChangesAsync(ct);
        return await IssueAsync(user, ct);
    }

    /// <summary>Rotación de refresh tokens: el usado se revoca y se emite uno nuevo.</summary>
    public async Task<AuthResponse> RefreshAsync(RefreshRequest req, CancellationToken ct)
    {
        var hash = TokenService.Hash(req.RefreshToken ?? string.Empty);
        var stored = await db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive || stored.User is null)
            throw new GameError(401, "bad_refresh", "Sesión expirada. Inicia sesión de nuevo.");

        stored.RevokedAtUtc = DateTime.UtcNow;
        return await IssueAsync(stored.User, ct);
    }

    private async Task<AuthResponse> IssueAsync(User user, CancellationToken ct)
    {
        var (plain, entity) = tokens.CreateRefreshToken(user.Id);
        db.RefreshTokens.Add(entity);

        // Higiene: elimina refresh tokens caducados/revocados del usuario.
        var cutoff = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == user.Id && (t.ExpiresAtUtc < cutoff || t.RevokedAtUtc != null))
            .ExecuteDeleteAsync(ct);

        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            tokens.CreateAccessToken(user), plain, tokens.AccessTokenSeconds,
            ProfileMapper.ToDto(user, _game, DateTime.UtcNow));
    }

    private User NewUser(string displayName)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            DisplayName = displayName,
            CreatedAtUtc = now,
            Lives = _game.MaxLives,
            LivesUpdatedAtUtc = now,
            Level = 1,
            WeekStartUtc = ProgressionLogic.WeekStart(DateOnly.FromDateTime(now))
        };
    }

    private static void ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || email.Length > 256)
            throw new GameError(400, "bad_email", "Correo inválido.");
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            throw new GameError(400, "weak_password", "La contraseña debe tener al menos 8 caracteres.");
    }

    private static string ValidateDisplayName(string name)
    {
        name = name.Trim();
        if (name.Length is < 2 or > 40)
            throw new GameError(400, "bad_name", "El nombre debe tener entre 2 y 40 caracteres.");
        return name;
    }
}
