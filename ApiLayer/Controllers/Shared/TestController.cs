using ApplicationLayer.Shared;
using Microsoft.AspNetCore.Mvc;
using ApiLayer.Tools;

namespace ApiLayer.Controllers.Shared;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IErrorLogService _errorLogService;

    public TestController(IErrorLogService errorLogService)
    {
        _errorLogService = errorLogService;
    }

    [HttpGet("success")]
    public ResponseStructure<string> GetSuccess()
    {
        return ResponseStructure<string>.Success("Operation completed successfully", "Success message");
    }

    [HttpGet("error")]
    public async Task<ResponseStructure<string>> GetError()
    {
        try
        {
            // Simular un error
            throw new InvalidOperationException("This is a test error for demonstration purposes");
        }
        catch (Exception ex)
        {
            // Log the error and get error number
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext);
            
            // Return error response with error number
            return ResponseStructure<string>.Error(
                "An error occurred during the operation. Please contact support with the error number.",
                500,
                errorNumber
            );
        }
    }

    [HttpGet("manual-error")]
    public async Task<ResponseStructure<string>> GetManualError()
    {
        // Log a manual error message
        var errorNumber = await _errorLogService.LogErrorAsync(
            "This is a manually logged error message", 
            HttpContext,
            "Additional context: User requested manual error test"
        );
        
        return ResponseStructure<string>.Error(
            "Manual error logged successfully. Please contact support with the error number.",
            500,
            errorNumber
        );
    }
} 