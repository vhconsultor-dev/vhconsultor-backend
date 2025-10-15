using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using ModelLayer;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

namespace ApiLayer.Controllers.Shared;

[ApiController]
[Route("api/[controller]")]
public class SummaryController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly IDatabaseConfigurationService _databaseConfigService;
    private readonly IConfiguration _configuration;
    private readonly DBcontext _dbContext;

    public SummaryController(
        IHostEnvironment environment,
        IDatabaseConfigurationService databaseConfigService,
        IConfiguration configuration,
        DBcontext dbContext)
    {
        _environment = environment;
        _databaseConfigService = databaseConfigService;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemSummary()
    {
        try
        {
            // Obtener versión de la aplicación desde appsettings
            var appVersion = _configuration["AppVersion"] ?? "1.0.0";
            
            // Probar conectividad a la base de datos (maneja sus propios errores)
            var databaseStatus = await TestDatabaseConnection();
            
            // Obtener versiones de las capas (no puede fallar)
            var layerVersions = GetLayerVersions();
            
            var summary = new
            {
                application = new
                {
                    name = "VHConsultor Backend",
                    version = appVersion,
                    environment = _environment.EnvironmentName,
                    isDevelopment = _environment.IsDevelopment(),
                    isProduction = _environment.IsProduction(),
                    isStaging = _environment.IsStaging()
                },
                database = new
                {
                    isConnected = databaseStatus.IsConnected,
                    connectionString = databaseStatus.ConnectionString,
                    server = databaseStatus.Server,
                    database = databaseStatus.Database,
                    status = databaseStatus.IsConnected ? "Connected" : "Disconnected",
                    error = databaseStatus.Error,
                    testTime = databaseStatus.TestTime
                },
                layers = new
                {
                    apiLayer = new
                    {
                        name = "ApiLayer",
                        version = layerVersions.ApiLayer,
                        description = "Capa de presentación - Controllers, Middleware"
                    },
                    applicationLayer = new
                    {
                        name = "ApplicationLayer", 
                        version = layerVersions.ApplicationLayer,
                        description = "Capa de aplicación - Services, DTOs, Validators"
                    },
                    businessLayer = new
                    {
                        name = "BusinessLayer",
                        version = layerVersions.BusinessLayer,
                        description = "Capa de lógica de negocio - Commands, Queries, Validators"
                    },
                    modelLayer = new
                    {
                        name = "ModelLayer",
                        version = layerVersions.ModelLayer,
                        description = "Capa de datos - Entities, DbContext, Configurations"
                    }
                },
                framework = new
                {
                    aspNetCore = typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly.GetName().Version?.ToString() ?? "Unknown",
                    csharp = "8.0", // .NET 8.0
                    dotnet = Environment.Version.ToString(),
                    entityFramework = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "Unknown"
                },
                system = new
                {
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    processorCount = Environment.ProcessorCount,
                    currentTime = DateTimeService.GetCostaRicaNow(),
                    timeZone = "Costa Rica (UTC-6)",
                    uptime = Environment.TickCount64
                }
            };

            // Determinar el mensaje basado en el estado de la base de datos
            var message = databaseStatus.IsConnected 
                ? "Información del sistema obtenida exitosamente" 
                : "Información del sistema obtenida exitosamente (Base de datos no disponible)";

            var response = ResponseStructure<object>.Success(summary, message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            // En caso de error crítico, devolver información básica del sistema
            var fallbackSummary = new
            {
                application = new
                {
                    name = "VHConsultor Backend",
                    version = _configuration["AppVersion"] ?? "1.0.0",
                    environment = _environment.EnvironmentName,
                    isDevelopment = _environment.IsDevelopment(),
                    isProduction = _environment.IsProduction(),
                    isStaging = _environment.IsStaging()
                },
                database = new
                {
                    isConnected = false,
                    connectionString = "Error retrieving connection string",
                    server = "Unknown",
                    database = "Unknown",
                    status = "Error",
                    error = $"Critical error: {ex.Message}",
                    testTime = DateTimeService.GetCostaRicaNow()
                },
                layers = new
                {
                    apiLayer = new { name = "ApiLayer", version = "Unknown", description = "Error retrieving version" },
                    applicationLayer = new { name = "ApplicationLayer", version = "Unknown", description = "Error retrieving version" },
                    businessLayer = new { name = "BusinessLayer", version = "Unknown", description = "Error retrieving version" },
                    modelLayer = new { name = "ModelLayer", version = "Unknown", description = "Error retrieving version" }
                },
                framework = new
                {
                    aspNetCore = "Unknown",
                    csharp = "8.0",
                    dotnet = Environment.Version.ToString(),
                    entityFramework = "Unknown"
                },
                system = new
                {
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    processorCount = Environment.ProcessorCount,
                    currentTime = DateTimeService.GetCostaRicaNow(),
                    timeZone = "Costa Rica (UTC-6)",
                    uptime = Environment.TickCount64
                }
            };

            var response = ResponseStructure<object>.Success(fallbackSummary, "Información del sistema obtenida con errores limitados");
            return Ok(response);
        }
    }

    private async Task<DatabaseStatus> TestDatabaseConnection()
    {
        var status = new DatabaseStatus
        {
            TestTime = DateTimeService.GetCostaRicaNow()
        };

        try
        {
            // Obtener información de la cadena de conexión
            var connectionString = _configuration.GetConnectionString("VH-DB");
            status.ConnectionString = connectionString?.Substring(0, Math.Min(50, connectionString.Length)) + "..." ?? "Not configured";
            
            // Extraer información del servidor y base de datos
            if (!string.IsNullOrEmpty(connectionString))
            {
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                        status.Server = part.Split('=')[1];
                    if (part.Trim().StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                        status.Database = part.Split('=')[1];
                }
            }

            // Probar la conexión con timeout más corto
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var canConnect = await _dbContext.Database.CanConnectAsync(cts.Token);
            status.IsConnected = canConnect;
        }
        catch (OperationCanceledException)
        {
            status.IsConnected = false;
            status.Error = "Connection timeout - Database server did not respond within 10 seconds";
        }
        catch (Exception ex)
        {
            status.IsConnected = false;
            status.Error = ex.Message;
        }

        return status;
    }

    private LayerVersions GetLayerVersions()
    {
        return new LayerVersions
        {
            ApiLayer = typeof(SummaryController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            ApplicationLayer = typeof(ApplicationLayer.Shared.ValidationService).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            BusinessLayer = typeof(BusinessLayer.Shared.Commands.ErrorLogCommandRepository).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            ModelLayer = typeof(ModelLayer.DBcontext).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        };
    }

    private class DatabaseStatus
    {
        public bool IsConnected { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string? Error { get; set; }
        public DateTime TestTime { get; set; }
    }

    private class LayerVersions
    {
        public string ApiLayer { get; set; } = string.Empty;
        public string ApplicationLayer { get; set; } = string.Empty;
        public string BusinessLayer { get; set; } = string.Empty;
        public string ModelLayer { get; set; } = string.Empty;
    }
} 