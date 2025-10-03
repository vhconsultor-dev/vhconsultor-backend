using ApplicationLayer.Shared;
using System.Net;
using System.Text.Json;

namespace ApiLayer.Tools;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IErrorLogService errorLogService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, errorLogService);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IErrorLogService errorLogService)
    {
        context.Response.ContentType = "application/json";
        
        // Log the error and get error number
        var errorNumber = await errorLogService.LogErrorAsync(exception, context);
        
        // Log to console as well
        _logger.LogError(exception, "Error logged with number: {ErrorNumber}", errorNumber);

        var response = ResponseStructure.Error(
            "An error occurred while processing your request. Please contact support with the error number.",
            500,
            errorNumber
        );

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
} 