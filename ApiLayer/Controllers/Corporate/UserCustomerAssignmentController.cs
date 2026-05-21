using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Asignación de usuarios corporate a clientes Brand Partner (y sus cuentas Amazon).
/// </summary>
[ApiController]
[Route("api/corporate/user-customer-assignments")]
[Authorize]
public class UserCustomerAssignmentController : ControllerBase
{
    private readonly UserCustomerAssignmentService _service;
    private readonly ValidationService _validationService;

    public UserCustomerAssignmentController(
        UserCustomerAssignmentService service,
        ValidationService validationService)
    {
        _service = service;
        _validationService = validationService;
    }

    /// <summary>
    /// Lista asignaciones con filtros opcionales. Incluye datos del usuario corporate,
    /// del customer y todas las AmazonAccounts del customer.
    /// </summary>
    /// <param name="userId">ID del usuario corporate (agente VH).</param>
    /// <param name="customerId">Filtrar por cliente.</param>
    /// <param name="id">ID de la asignación.</param>
    /// <param name="isActive">Filtrar asignaciones activas/inactivas.</param>
    [HttpGet]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] int? userId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] int? id = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAssignmentsAsync(id, userId, customerId, isActive);

            if (id.HasValue && items.Count == 0)
            {
                var notFound = ResponseStructure<object>.NotFound(
                    $"Assignment with ID {id.Value} was not found.");
                return NotFound(notFound);
            }

            var message = items.Count == 1 && id.HasValue
                ? "Assignment retrieved successfully."
                : $"Found {items.Count} assignment(s).";

            var response = ResponseStructure<object>.Success(items, message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error retrieving assignments: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Asigna un usuario corporate a un cliente.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateUserCustomerAssignmentRequest request)
    {
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            var assignmentId = await _service.CreateAsync(request);
            var created = await _service.GetAssignmentsAsync(userCustomerAssignmentId: assignmentId);
            var data = created.FirstOrDefault();

            var successResponse = ResponseStructure<object>.Success(
                new { userCustomerAssignmentId = assignmentId, assignment = data },
                "Customer assigned to corporate user successfully.");
            return Ok(successResponse);
        }
        catch (InvalidOperationException ex)
        {
            var errorResponse = ResponseStructure<object>.Error(ex.Message, 400);
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error creating assignment: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Actualiza una asignación (activar / inactivar).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAssignment(
        int id,
        [FromBody] UpdateUserCustomerAssignmentRequest request)
    {
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            var updated = await _service.UpdateAsync(id, request);
            if (!updated)
            {
                var notFound = ResponseStructure<object>.NotFound(
                    $"Assignment with ID {id} was not found.");
                return NotFound(notFound);
            }

            var detail = await _service.GetAssignmentsAsync(userCustomerAssignmentId: id);
            var message = request.IsActive
                ? "Assignment activated successfully."
                : "Assignment inactivated successfully.";

            var successResponse = ResponseStructure<object>.Success(
                detail.FirstOrDefault(),
                message);
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error updating assignment: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }
}
