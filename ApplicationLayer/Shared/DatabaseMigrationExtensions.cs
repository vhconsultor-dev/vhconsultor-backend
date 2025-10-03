using Microsoft.Extensions.DependencyInjection;

namespace ApplicationLayer.Shared;

public static class DatabaseMigrationExtensions
{
    public static IServiceCollection AddDatabaseMigrationService(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        return services;
    }
} 