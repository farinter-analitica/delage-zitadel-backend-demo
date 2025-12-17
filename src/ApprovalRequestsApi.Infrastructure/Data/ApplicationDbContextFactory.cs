using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApprovalRequestsApi.Infrastructure.Data;

/// <summary>
/// Factory para crear el DbContext en tiempo de diseño (para migraciones).
/// Esto permite ejecutar migraciones sin tener que configurar Zitadel.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Usar una cadena de conexión por defecto para migraciones
        // Esto se puede sobrescribir con la variable de entorno EF_DESIGN_CONNECTION_STRING
        var connectionString = Environment.GetEnvironmentVariable("EF_DESIGN_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=approval_requests_db;Username=postgres;Password=postgres_password";

        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
