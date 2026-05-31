using BusinessLayer.Shared.Commands;
using ModelLayer.Shared.Entities;
using ModelLayer.Shared;
using Microsoft.AspNetCore.Http;

namespace ApplicationLayer.Shared;

public interface IErrorLogService
{
    Task<string> LogErrorAsync(Exception exception, HttpContext? httpContext = null, string? additionalData = null);
    Task<string> LogErrorAsync(string message, HttpContext? httpContext = null, string? additionalData = null);
}

public class ErrorLogService : IErrorLogService
{
    private readonly IErrorLogCommandRepository _errorLogRepository;

    public ErrorLogService(IErrorLogCommandRepository errorLogRepository)
    {
        _errorLogRepository = errorLogRepository;
    }

    public async Task<string> LogErrorAsync(Exception exception, HttpContext? httpContext = null, string? additionalData = null)
    {
        var errorLog = new ErrorLog
        {
            ErrorNumber = GenerateErrorNumber(),
            Message = GetFullExceptionMessage(exception),
            StackTrace = exception.StackTrace,
            Source = exception.Source,
            ExceptionType = exception.GetType().Name,
            CreatedAt = DateTimeService.GetCostaRicaNow(),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            AdditionalData = additionalData
        };

        // Agregar información del HTTP context si está disponible
        if (httpContext != null)
        {
            errorLog.RequestPath = httpContext.Request.Path;
            errorLog.RequestMethod = httpContext.Request.Method;
            errorLog.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
            errorLog.QueryString = httpContext.Request.QueryString.ToString();
            
            // Obtener UserId si está autenticado
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                errorLog.UserId = httpContext.User.FindFirst("sub")?.Value ?? 
                                 httpContext.User.FindFirst("nameid")?.Value;
            }

            // Obtener RequestBody si es posible
            if (httpContext.Request.Body.CanSeek)
            {
                httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(httpContext.Request.Body);
                errorLog.RequestBody = await reader.ReadToEndAsync();
            }
        }

        var id = await _errorLogRepository.CreateAsync(errorLog);
        return errorLog.ErrorNumber;
    }

    public async Task<string> LogErrorAsync(string message, HttpContext? httpContext = null, string? additionalData = null)
    {
        var errorLog = new ErrorLog
        {
            ErrorNumber = GenerateErrorNumber(),
            Message = message,
            CreatedAt = DateTimeService.GetCostaRicaNow(),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            AdditionalData = additionalData
        };

        // Agregar información del HTTP context si está disponible
        if (httpContext != null)
        {
            errorLog.RequestPath = httpContext.Request.Path;
            errorLog.RequestMethod = httpContext.Request.Method;
            errorLog.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
            errorLog.QueryString = httpContext.Request.QueryString.ToString();
            
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                errorLog.UserId = httpContext.User.FindFirst("sub")?.Value ?? 
                                 httpContext.User.FindFirst("nameid")?.Value;
            }
        }

        var id = await _errorLogRepository.CreateAsync(errorLog);
        return errorLog.ErrorNumber;
    }

    private string GenerateErrorNumber()
    {
        // Generar un número de error único: ERR-YYYYMMDD-HHMMSS-XXXX
        var timestamp = DateTimeService.GetCostaRicaNow().ToString("yyyyMMdd-HHmmss");
        var random = new Random();
        var randomPart = random.Next(1000, 9999).ToString();
        return $"ERR-{timestamp}-{randomPart}";
    }

    private static string GetFullExceptionMessage(Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current != null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
                messages.Add(current.Message);
            current = current.InnerException;
        }

        return string.Join(" | ", messages.Distinct());
    }
} 