using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelLayer;

namespace ApplicationLayer.Shared;

public interface IDatabaseMigrationService
{
    Task MigrateAsync();
    Task EnsureDatabaseCreatedAsync();
}

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(IServiceProvider serviceProvider, ILogger<DatabaseMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task MigrateAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DBcontext>();
            
            _logger.LogInformation("Iniciando migración de base de datos...");
            
            // Aplicar todas las migraciones pendientes
            await context.Database.MigrateAsync();
            
            _logger.LogInformation("Migración de base de datos completada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la migración de base de datos");
            throw;
        }
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DBcontext>();
            
            _logger.LogInformation("Verificando existencia de base de datos...");
            
            // Crear la base de datos si no existe
            await context.Database.EnsureCreatedAsync();
            
            _logger.LogInformation("Base de datos verificada/creada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar/crear la base de datos");
            throw;
        }
    }
} 