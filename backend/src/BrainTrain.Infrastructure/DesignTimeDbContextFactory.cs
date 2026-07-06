using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BrainTrain.Infrastructure;

/// <summary>
/// Solo para `dotnet ef migrations add` en tiempo de diseño. Las migraciones
/// se generan contra el proveedor de producción (PostgreSQL).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=braintrain_design;Username=design;Password=design")
            .Options;
        return new AppDbContext(options);
    }
}
