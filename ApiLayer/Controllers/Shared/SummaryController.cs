using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using ApplicationLayer.Shared;
using ApiLayer.Tools;

namespace ApiLayer.Controllers.Shared;

[ApiController]
[Route("api/[controller]")]
public class SummaryController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly IDatabaseConfigurationService _databaseConfigService;

    public SummaryController(
        IHostEnvironment environment,
        IDatabaseConfigurationService databaseConfigService)
    {
        _environment = environment;
        _databaseConfigService = databaseConfigService;
    }

    [HttpGet]
    public IActionResult GetSystemSummary()
    {
        var summary = new
        {
            environment = new
            {
                name = _environment.EnvironmentName,
                isDevelopment = _environment.IsDevelopment(),
                isProduction = _environment.IsProduction(),
                isStaging = _environment.IsStaging()
            },
            versions = new
            {
                aspNetCore = typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly.GetName().Version?.ToString() ?? "Unknown",
                csharp = "8.0", // .NET 8.0
                dotnet = Environment.Version.ToString()
            },
            database = new
            {
                usingLegacy = _databaseConfigService.IsUsingLegacyDatabase(),
                databaseType = _databaseConfigService.GetCurrentDatabaseType()
            },
            system = new
            {
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                processorCount = Environment.ProcessorCount,
                currentTime = DateTime.UtcNow,
                timeZone = TimeZoneInfo.Local.DisplayName
            }
        };

        var response = ResponseStructure<object>.Success(summary, "Información del sistema obtenida exitosamente");
        return Ok(response);
    }
} 