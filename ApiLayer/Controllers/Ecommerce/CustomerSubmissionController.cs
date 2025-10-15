using ApplicationLayer.Ecommerce;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Ecommerce.Commands;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Ecommerce;

[ApiController]
[Route("api/[controller]")]
public class CustomerSubmissionController : ControllerBase
{
    private readonly CustomerSubmissionService _customerSubmissionService;
    private readonly ValidationService _validationService;

    public CustomerSubmissionController(
        CustomerSubmissionService customerSubmissionService,
        ValidationService validationService)
    {
        _customerSubmissionService = customerSubmissionService;
        _validationService = validationService;
    }

    #region POST - Create Customer Submission

    /// <summary>
    /// Crea una nueva Customer Submission
    /// </summary>
    /// <param name="request">Datos de la submission</param>
    /// <returns>ID de la submission creada</returns>
    [HttpPost]
    public async Task<IActionResult> CreateCustomerSubmission([FromBody] CreateCustomerSubmissionRequest request)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            var submissionId = await _customerSubmissionService.CreateCustomerSubmissionAsync(request);
            
            var successResponse = ResponseStructure<int>.Success(
                submissionId, 
                "Customer Submission creada exitosamente");
            
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al crear la Customer Submission: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

