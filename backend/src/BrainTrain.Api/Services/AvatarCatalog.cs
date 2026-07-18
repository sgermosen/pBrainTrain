using BrainTrain.Domain;

namespace BrainTrain.Api.Services;

public sealed record AvatarDef(string Code, int PriceCoins);

/// <summary>
/// Catálogo de avatares: 10 gratis + premium comprables con monedas ganadas
/// jugando (cosméticos como destino de la economía; nunca ventaja de juego).
/// </summary>
public static class AvatarCatalog
{
    public static readonly IReadOnlyList<AvatarDef> All =
    [
        new("fox", 0), new("owl", 0), new("cat", 0), new("panda", 0), new("robot", 0),
        new("alien", 0), new("lion", 0), new("penguin", 0), new("koala", 0), new("dragon", 0),
        new("monkey", 120), new("unicorn", 150), new("tiger", 150),
        new("wolf", 200), new("octopus", 200), new("butterfly", 250),
    ];

    public static AvatarDef? Find(string code) => All.FirstOrDefault(a => a.Code == code);

    public static bool Owns(User user, string code)
    {
        var def = Find(code);
        if (def is null) return false;
        if (def.PriceCoins == 0) return true;
        return user.OwnedAvatarsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(code);
    }
}
