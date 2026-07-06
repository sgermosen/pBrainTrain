using BrainTrain.Api.Services;

namespace BrainTrain.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/auth").RequireRateLimiting("auth");

        g.MapPost("/register", async (RegisterRequest req, AuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.RegisterAsync(req, ct)));

        g.MapPost("/login", async (LoginRequest req, AuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.LoginAsync(req, ct)));

        g.MapPost("/guest", async (GuestRequest req, AuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.GuestAsync(req, ct)));

        g.MapPost("/refresh", async (RefreshRequest req, AuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.RefreshAsync(req, ct)));

        g.MapPost("/upgrade", async (UpgradeRequest req, HttpContext http, AuthService auth, CancellationToken ct) =>
                Results.Ok(await auth.UpgradeAsync(http.User.UserId(), req, ct)))
            .RequireAuthorization();
    }
}
