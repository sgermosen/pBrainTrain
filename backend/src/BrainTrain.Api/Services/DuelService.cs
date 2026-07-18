using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

/// <summary>
/// Duelos asíncronos 1v1: crear (con código para compartir o abierto al
/// público), unirse y listar. El puntaje lo escribe GameService al enviar la
/// partida; el ganador recibe un bonus de XP.
/// </summary>
public sealed class DuelService(
    AppDbContext db,
    QuestionCatalog catalog,
    IOptions<GameOptions> options)
{
    public const int WinnerBonusXp = 20;
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private readonly GameOptions _opt = options.Value;

    public async Task<DuelStartResponse> CreateAsync(long userId, bool openToPublic, CancellationToken ct)
    {
        var user = await SpendLifeAsync(userId, ct);
        var snap = await catalog.GetAsync(ct);
        var ids = GameService.PickQuestions(snap, snap.AllQuestionIds, _opt.QuestionsPerSession, 2.5, Random.Shared);

        var duel = new Duel
        {
            Id = Guid.NewGuid(),
            Code = GenerateCode(),
            ChallengerUserId = userId,
            QuestionIdsCsv = string.Join(',', ids),
            TotalCount = ids.Length,
            IsOpenToPublic = openToPublic,
            Status = DuelStatus.WaitingOpponent,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Duels.Add(duel);

        var session = NewSession(userId, duel);
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(ct);

        return await BuildStartResponseAsync(duel, session, user, snap, ct);
    }

    public async Task<DuelStartResponse> JoinByCodeAsync(long userId, string code, CancellationToken ct)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        var duel = await db.Duels.FirstOrDefaultAsync(d => d.Code == code, ct)
                   ?? throw new GameError(404, "duel_not_found", "No existe un duelo con ese código.");
        return await JoinAsync(userId, duel, ct);
    }

    /// <summary>Emparejamiento aleatorio: entra al duelo público más antiguo o crea uno nuevo.</summary>
    public async Task<DuelStartResponse> JoinRandomAsync(long userId, CancellationToken ct)
    {
        var open = await db.Duels
            .Where(d => d.IsOpenToPublic && d.Status == DuelStatus.WaitingOpponent && d.ChallengerUserId != userId)
            .OrderBy(d => d.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        return open is null
            ? await CreateAsync(userId, openToPublic: true, ct)
            : await JoinAsync(userId, open, ct);
    }

    private async Task<DuelStartResponse> JoinAsync(long userId, Duel duel, CancellationToken ct)
    {
        if (duel.ChallengerUserId == userId)
            throw new GameError(409, "own_duel", "No puedes jugar contra ti. Comparte el código 😉");
        if (duel.Status != DuelStatus.WaitingOpponent)
            throw new GameError(409, "duel_taken", "Ese duelo ya tiene rival.");

        var user = await SpendLifeAsync(userId, ct);
        duel.OpponentUserId = userId;
        duel.Status = DuelStatus.InProgress;

        var session = NewSession(userId, duel);
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(ct);

        var snap = await catalog.GetAsync(ct);
        return await BuildStartResponseAsync(duel, session, user, snap, ct);
    }

    public async Task<List<DuelDto>> MineAsync(long userId, CancellationToken ct)
    {
        var duels = await db.Duels.AsNoTracking()
            .Include(d => d.Challenger)
            .Include(d => d.Opponent)
            .Where(d => d.ChallengerUserId == userId || d.OpponentUserId == userId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(20)
            .ToListAsync(ct);
        return duels.Select(d => ToDto(d, userId)).ToList();
    }

    public static DuelDto ToDto(Duel d, long viewerId)
    {
        var iAmChallenger = d.ChallengerUserId == viewerId;
        return new DuelDto(
            d.Id, d.Code, d.Status,
            ChallengerName: d.Challenger?.DisplayName ?? "?",
            OpponentName: d.Opponent?.DisplayName,
            MyScore: iAmChallenger ? d.ChallengerScore : d.OpponentScore,
            TheirScore: iAmChallenger ? d.OpponentScore : d.ChallengerScore,
            IAmChallenger: iAmChallenger,
            TotalCount: d.TotalCount,
            CreatedAtUtc: d.CreatedAtUtc);
    }

    // ------------------------------------------------------------- helpers
    private async Task<User> SpendLifeAsync(long userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new GameError(401, "user_not_found", "Usuario no encontrado.");
        var now = DateTime.UtcNow;
        var (maxLives, regen) = ProgressionLogic.LivesParams(user, _opt, now);
        var (lives, anchor, _) = ProgressionLogic.ComputeLives(user.Lives, user.LivesUpdatedAtUtc, now, maxLives, regen);
        if (lives <= 0)
            throw new GameError(409, "no_lives", "Sin vidas. Espera la recarga, canjea monedas o visita la tienda.");
        user.Lives = lives - 1;
        user.LivesUpdatedAtUtc = user.Lives >= maxLives ? now : anchor;
        return user;
    }

    private static GameSession NewSession(long userId, Duel duel) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Mode = GameMode.Duel,
        DuelId = duel.Id,
        QuestionIdsCsv = duel.QuestionIdsCsv,
        StartedAtUtc = DateTime.UtcNow,
        TotalCount = duel.TotalCount
    };

    private async Task<DuelStartResponse> BuildStartResponseAsync(
        Duel duel, GameSession session, User user, CatalogSnapshot snap, CancellationToken ct)
    {
        // Carga nombres para el DTO (challenger/opponent pueden ser el propio user).
        var loaded = await db.Duels.AsNoTracking()
            .Include(d => d.Challenger).Include(d => d.Opponent)
            .FirstAsync(d => d.Id == duel.Id, ct);

        var now = DateTime.UtcNow;
        var (maxLives, regen) = ProgressionLogic.LivesParams(user, _opt, now);
        var (lives, _, secs) = ProgressionLogic.ComputeLives(user.Lives, user.LivesUpdatedAtUtc, now, maxLives, regen);

        var questions = GameService.ParseIds(duel.QuestionIdsCsv)
            .Where(snap.QuestionsById.ContainsKey)
            .Select(i => snap.QuestionsById[i].ToDto()).ToList();

        return new DuelStartResponse(ToDto(loaded, user.Id), session.Id, questions,
            new LivesDto(lives, maxLives, secs));
    }

    private string GenerateCode()
    {
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < 6; i++)
            chars[i] = CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)];
        return new string(chars);
    }
}
