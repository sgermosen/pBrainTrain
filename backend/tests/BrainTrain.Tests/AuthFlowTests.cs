using System.Net;
using BrainTrain.Api;

namespace BrainTrain.Tests;

public class AuthFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthFlowTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Salud_Y_Raiz_Responden()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task Invitado_EntraYObtienePerfilConVidasLlenas()
    {
        var client = _factory.CreateClient();
        var auth = await client.NewGuestAsync("Peque");

        Assert.True(auth.Profile.IsGuest);
        Assert.Equal("Peque", auth.Profile.DisplayName);
        Assert.Equal(5, auth.Profile.Lives.Current);
        Assert.Equal(1, auth.Profile.Level);

        var me = await (await client.GetAsync("/api/v1/me")).ReadAs<ProfileDto>();
        Assert.Equal(auth.Profile.Id, me.Id);
    }

    [Fact]
    public async Task Invitado_MismoDispositivoRecuperaLaMismaCuenta()
    {
        var client = _factory.CreateClient();
        var device = $"device-{Guid.NewGuid():N}";
        var a = await (await client.PostJson("/api/v1/auth/guest", new GuestRequest(device, null))).ReadAs<AuthResponse>();
        var b = await (await client.PostJson("/api/v1/auth/guest", new GuestRequest(device, null))).ReadAs<AuthResponse>();
        Assert.Equal(a.Profile.Id, b.Profile.Id);
    }

    [Fact]
    public async Task Registro_Login_Refresh_Funcionan()
    {
        var client = _factory.CreateClient();
        var email = $"user{Guid.NewGuid():N}@test.com";

        var reg = await (await client.PostJson("/api/v1/auth/register",
            new RegisterRequest(email, "clave-segura-123", "Ana"))).ReadAs<AuthResponse>();
        Assert.False(reg.Profile.IsGuest);

        // Email duplicado → 409
        var dup = await client.PostJson("/api/v1/auth/register", new RegisterRequest(email, "clave-segura-123", "Otro"));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var login = await (await client.PostJson("/api/v1/auth/login",
            new LoginRequest(email, "clave-segura-123"))).ReadAs<AuthResponse>();
        Assert.Equal(reg.Profile.Id, login.Profile.Id);

        // Contraseña incorrecta → 401
        var bad = await client.PostJson("/api/v1/auth/login", new LoginRequest(email, "incorrecta-123"));
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);

        // Rotación de refresh: el nuevo funciona, el usado queda revocado.
        var refreshed = await (await client.PostJson("/api/v1/auth/refresh",
            new RefreshRequest(login.RefreshToken))).ReadAs<AuthResponse>();
        Assert.Equal(reg.Profile.Id, refreshed.Profile.Id);
        var reuse = await client.PostJson("/api/v1/auth/refresh", new RefreshRequest(login.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Invitado_AsciendeACuentaCompletaConservandoProgreso()
    {
        var client = _factory.CreateClient();
        var guest = await client.NewGuestAsync();

        var email = $"up{Guid.NewGuid():N}@test.com";
        var upgraded = await (await client.PostJson("/api/v1/auth/upgrade",
            new UpgradeRequest(email, "clave-segura-123"))).ReadAs<AuthResponse>();

        Assert.Equal(guest.Profile.Id, upgraded.Profile.Id);
        Assert.False(upgraded.Profile.IsGuest);
        Assert.Equal(email, upgraded.Profile.Email);
    }

    [Fact]
    public async Task SinToken_LosEndpointsProtegidosDevuelven401()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostJson("/api/v1/game/start", new StartGameRequest(BrainTrain.Domain.GameMode.Quick, null))).StatusCode);
    }

    [Fact]
    public async Task Perfil_SePuedeEditarConValidacion()
    {
        var client = _factory.CreateClient();
        await client.NewGuestAsync();

        var patch = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/me")
        {
            Content = System.Net.Http.Json.JsonContent.Create(
                new UpdateProfileRequest("Nuevo Nombre", "owl"), options: TestClientExtensions.Json)
        };
        var updated = await (await client.SendAsync(patch)).ReadAs<ProfileDto>();
        Assert.Equal("Nuevo Nombre", updated.DisplayName);
        Assert.Equal("owl", updated.AvatarCode);

        var badPatch = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/me")
        {
            Content = System.Net.Http.Json.JsonContent.Create(
                new UpdateProfileRequest(null, "no-existe"), options: TestClientExtensions.Json)
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(badPatch)).StatusCode);
    }
}
