using System.Text;
using System.Threading.RateLimiting;
using BrainTrain.Api;
using BrainTrain.Api.Endpoints;
using BrainTrain.Api.Services;
using BrainTrain.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging: Serilog, ruido de framework a Warning ----------
builder.Host.UseSerilog((ctx, cfg) => cfg
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console());

// ---------- Opciones ----------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.Configure<GameOptions>(builder.Configuration.GetSection(GameOptions.Section));
builder.Services.Configure<StoreOptions>(builder.Configuration.GetSection(StoreOptions.Section));
builder.Services.Configure<PayPalOptions>(builder.Configuration.GetSection(PayPalOptions.Section));
builder.Services.Configure<PushOptions>(builder.Configuration.GetSection(PushOptions.Section));

// ---------- Base de datos: SQLite (dev/pruebas) o PostgreSQL (producción) ----------
var provider = builder.Configuration["Database:Provider"] ?? "sqlite";
builder.Services.AddDbContextPool<AppDbContext>(opt =>
{
    if (provider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
    else
        opt.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=braintrain.db");
});

// ---------- Autenticación JWT ----------
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    if (builder.Environment.IsDevelopment())
        // Clave SOLO para desarrollo local; producción exige Jwt__Key por variable de entorno.
        builder.Configuration["Jwt:Key"] = jwtKey = "dev-only-key-do-not-use-in-production-0123456789";
    else
        throw new InvalidOperationException(
            "Jwt:Key no configurada. Define la variable de entorno Jwt__Key con una clave aleatoria de 64+ caracteres.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "braintrain",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "braintrain-app",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---------- Serialización JSON con source generators (menos CPU y memoria) ----------
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default));

// ---------- Caches ----------
builder.Services.AddMemoryCache();
builder.Services.AddOutputCache(o =>
{
    // Contenido público idéntico para todos (categorías, catálogo de tienda).
    // excludeDefaultPolicy: cachea aunque el cliente envíe header Authorization.
    o.AddPolicy("content", p => p.Cache().Expire(TimeSpan.FromMinutes(5)), excludeDefaultPolicy: true);
});

// ---------- Rate limiting: protege el VPS de abuso y de picos ----------
var globalPerMinute = builder.Configuration.GetValue("RateLimiting:GlobalPerMinute", 120);
var authPerMinute = builder.Configuration.GetValue("RateLimiting:AuthPerMinute", 15);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = globalPerMinute,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));
    // Los endpoints de credenciales llevan un límite mucho más estricto (fuerza bruta).
    o.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------- Compresión de respuestas (JSON comprime ~80%) ----------
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});

// ---------- Servicios de la aplicación ----------
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<QuestionCatalog>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<StoreService>();
builder.Services.AddSingleton<IPurchaseVerifier, DefaultPurchaseVerifier>();
builder.Services.AddScoped<MinigameService>();
builder.Services.AddScoped<QuestService>();
builder.Services.AddScoped<DuelService>();
builder.Services.AddScoped<PayPalCheckoutService>();
builder.Services.AddHttpClient<FcmPushSender>();
builder.Services.AddSingleton<IPushSender>(sp => sp.GetRequiredService<FcmPushSender>());
builder.Services.AddHostedService<StreakReminderService>();
builder.Services.AddHttpClient<HttpPayPalGateway>();
builder.Services.AddSingleton<IPayPalGateway>(sp =>
    sp.GetRequiredService<HttpPayPalGateway>());

builder.Services.AddHealthChecks().AddCheck<DbHealthCheck>("database");
builder.Services.AddScoped<DbHealthCheck>();

// Detrás de nginx: respeta X-Forwarded-For/Proto para IPs reales y rate limiting correcto.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

// Cuerpos pequeños: la API solo recibe JSON compactos.
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 64 * 1024);

var app = builder.Build();

// ---------- Migración y seed al arrancar ----------
if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (provider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    if (app.Configuration.GetValue("Database:SeedOnStartup", true))
        await SeedService.SeedAsync(db, app.Environment.ContentRootPath,
            scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}

app.UseForwardedHeaders();
app.UseResponseCompression();
// Portal web de pagos (wwwroot/portal): página estática con PayPal JS SDK.
app.UseStaticFiles();
app.Use(EndpointHelpers.GameErrorMiddleware);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapGet("/", (IWebHostEnvironment env) =>
    Results.Ok(new ApiInfoDto("BrainTrain API", "1.0.0", env.EnvironmentName)));
app.MapHealthChecks("/health");

app.MapAuth();
app.MapMe();
app.MapGame();
app.MapContent();
app.MapStore();
app.MapExtras();
app.MapSocial();
app.MapAdmin();
app.MapGet("/portal", () => Results.Redirect("/portal/index.html"));
app.MapGet("/admin", () => Results.Redirect("/admin/index.html"));

app.Run();

/// <summary>Health check ligero: una consulta mínima a la base.</summary>
public sealed class DbHealthCheck(AppDbContext db) : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, CancellationToken ct = default)
        => await db.Database.CanConnectAsync(ct)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Sin conexión a la base de datos");
}

// Necesario para WebApplicationFactory en las pruebas de integración.
public partial class Program;
