using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controller for Amazon Account–Marketplace associations (GET with optional filters, POST, PUT, DELETE).
/// Links Amazon accounts to marketplaces; one account can have many marketplaces.
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class AmazonAccountMarketplaceController : ControllerBase
{
    private readonly AmazonAccountMarketplaceService _service;
    private readonly ValidationService _validationService;

    public AmazonAccountMarketplaceController(
        AmazonAccountMarketplaceService service,
        ValidationService validationService)
    {
        _service = service;
        _validationService = validationService;
    }

    /// <summary>
    /// Gets account–marketplace associations with optional query parameters. When multiple parameters are sent, filters are combined with AND. All filters use exact match. When no parameters are sent, all associations are returned.
    /// </summary>
    /// <param name="id">Optional. Return only the association with this ID.</param>
    /// <param name="amazonAccountId">Optional. Filter by Amazon account ID.</param>
    /// <param name="amazonMarketplaceId">Optional. Filter by Amazon marketplace ID.</param>
    /// <param name="isPrimary">Optional. Filter by primary flag.</param>
    /// <param name="isActive">Optional. Filter by active status.</param>
    [HttpGet]
    public async Task<IActionResult> GetAmazonAccountMarketplaces(
        [FromQuery] int? id = null,
        [FromQuery] int? amazonAccountId = null,
        [FromQuery] int? amazonMarketplaceId = null,
        [FromQuery] bool? isPrimary = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAmazonAccountMarketplacesAsync(
                id, amazonAccountId, amazonMarketplaceId, isPrimary, isActive);

            if (id.HasValue && !items.Any())
            {
                var notFoundResponse = ResponseStructure<object>.Error(
                    $"Amazon account–marketplace association with ID {id.Value} was not found.",
                    404);
                return NotFound(notFoundResponse);
            }

            var message = items.Count() == 1 && id.HasValue
                ? "Amazon account–marketplace association retrieved successfully."
                : $"Found {items.Count()} Amazon account–marketplace association(s).";
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.AmazonAccountMarketplace>>.Success(items, message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error retrieving Amazon account–marketplace associations: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Creates a new association linking an Amazon account to a marketplace. Amazon account and marketplace must exist; the pair must not already be linked.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAmazonAccountMarketplace([FromBody] CreateAmazonAccountMarketplaceRequest request)
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
            var associationId = await _service.CreateAsync(request);
            var successResponse = ResponseStructure<int>.Success(associationId, "Amazon account–marketplace association created successfully.");
            return Ok(successResponse);
        }
        catch (InvalidOperationException ex)
        {
            var errorResponse = ResponseStructure<object>.Error(ex.Message, 400);
            return BadRequest(errorResponse);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var errorResponse = ResponseStructure<object>.Error(
                $"Error creating Amazon account–marketplace association: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error creating Amazon account–marketplace association: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Updates an existing association by ID. Only IsPrimary and IsActive can be updated.
    /// </summary>
    [HttpPut("{amazonAccountMarketplaceId}")]
    public async Task<IActionResult> UpdateAmazonAccountMarketplace(int amazonAccountMarketplaceId, [FromBody] UpdateAmazonAccountMarketplaceRequest request)
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
            var updated = await _service.UpdateAsync(amazonAccountMarketplaceId, request);
            if (!updated)
            {
                var notFoundResponse = ResponseStructure<object>.Error(
                    $"Amazon account–marketplace association with ID {amazonAccountMarketplaceId} was not found.",
                    404);
                return NotFound(notFoundResponse);
            }
            var successResponse = ResponseStructure.Success("Amazon account–marketplace association updated successfully.");
            return Ok(successResponse);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var errorResponse = ResponseStructure<object>.Error(
                $"Error updating Amazon account–marketplace association: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error updating Amazon account–marketplace association: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Deletes an association by ID. The link between the Amazon account and the marketplace is removed.
    /// </summary>
    [HttpDelete("{amazonAccountMarketplaceId}")]
    public async Task<IActionResult> DeleteAmazonAccountMarketplace(int amazonAccountMarketplaceId)
    {
        try
        {
            var deleted = await _service.DeleteAsync(amazonAccountMarketplaceId);
            if (!deleted)
            {
                var notFoundResponse = ResponseStructure<object>.Error(
                    $"Amazon account–marketplace association with ID {amazonAccountMarketplaceId} was not found.",
                    404);
                return NotFound(notFoundResponse);
            }
            var successResponse = ResponseStructure.Success("Amazon account–marketplace association deleted successfully.");
            return Ok(successResponse);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var errorResponse = ResponseStructure<object>.Error(
                $"Error deleting Amazon account–marketplace association: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error deleting Amazon account–marketplace association: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }
}
