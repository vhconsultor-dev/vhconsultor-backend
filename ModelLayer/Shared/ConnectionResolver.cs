using Microsoft.Extensions.Configuration;

namespace ModelLayer.Shared;

public interface IConnectionResolver
{
    string GetConnectionString(string connectionName);
}

public class ConnectionResolver : IConnectionResolver
{
    private readonly IConfiguration _configuration;
    private readonly bool _isDevelopment;

    public ConnectionResolver(IConfiguration configuration, bool isDevelopment = true)
    {
        _configuration = configuration;
        _isDevelopment = isDevelopment;
    }

    public string GetConnectionString(string connectionName)
    {
        try
        {
            if (_isDevelopment)
            {
                return GetDevelopmentConnectionString(connectionName);
            }
            else
            {
                return GetProductionConnectionString(connectionName);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error obteniendo cadena de conexión para: {connectionName}", ex);
        }
    }

    private string GetDevelopmentConnectionString(string connectionName)
    {
        var connectionString = _configuration.GetConnectionString(connectionName);
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException($"Cadena de conexión '{connectionName}' no encontrada en Development");
        }
        
        return connectionString;
    }

    private string GetProductionConnectionString(string connectionName)
    {
        // TODO: Implementar lógica para obtener desde Azure KeyVault
        // Por ahora, usamos las cadenas de appsettings como fallback
        
        var connectionString = _configuration.GetConnectionString(connectionName);
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException($"Cadena de conexión '{connectionName}' no encontrada en Production");
        }
        
        return connectionString;
    }
} 