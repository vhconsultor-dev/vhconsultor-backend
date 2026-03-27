using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controller for Amazon Marketplaces maintenance (GET with optional filters, POST, PUT). No DELETE.
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class AmazonMarketplaceController : ControllerBase
{
    private readonly AmazonMarketplaceService _amazonMarketplaceService;
    private readonly ValidationService _validationService;

    public AmazonMarketplaceController(
        AmazonMarketplaceService amazonMarketplaceService,
        ValidationService validationService)
    {
        _amazonMarketplaceService = amazonMarketplaceService;
        _validationService = validationService;
    }

    /// <summary>
    /// Gets Amazon marketplaces with optional query parameters. When multiple parameters are sent, filters are combined with AND.
    /// String parameters use partial match (LIKE). When no parameters are sent, all marketplaces are returned.
    /// </summary>
    /// <param name="id">Optional. Return only the marketplace with this ID.</param>
    /// <param name="amazonMarketplaceCode">Optional. Partial match on marketplace code.</param>
    /// <param name="countryCode">Optional. Partial match on country code.</param>
    /// <param name="countryName">Optional. Partial match on country name.</param>
    /// <param name="amazonRegion">Optional. Partial match on Amazon region (e.g. NA, EU, FE).</param>
    /// <param name="currencyCode">Optional. Partial match on currency code.</param>
    /// <param name="isActive">Optional. Filter by active status (true/false).</param>
    [HttpGet]
    [RequirePermission("corporate.settings.amazon_marketplaces.read")]
    public async Task<IActionResult> GetAmazonMarketplaces(
        [FromQuery] int? id = null,
        [FromQuery] string? amazonMarketplaceCode = null,
        [FromQuery] string? countryCode = null,
        [FromQuery] string? countryName = null,
        [FromQuery] string? amazonRegion = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _amazonMarketplaceService.GetAmazonMarketplacesAsync(
                id, amazonMarketplaceCode, countryCode, countryName, amazonRegion, currencyCode, isActive);

            if (id.HasValue && !items.Any())
            {
                var notFoundResponse = ResponseStructure<object>.Error(
                    $"Amazon marketplace with ID {id.Value} was not found.",
                    404);
                return NotFound(notFoundResponse);
            }

            var message = items.Count() == 1 && id.HasValue
                ? "Amazon marketplace retrieved successfully."
                : $"Found {items.Count()} Amazon marketplace(s).";
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.AmazonMarketplace>>.Success(items, message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error retrieving Amazon marketplaces: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Creates a new Amazon marketplace.
    /// </summary>
    /// <param name="request">Marketplace data (AmazonMarketplaceCode, CountryCode, CountryName, AmazonRegion, CurrencyCode, IsActive).</param>
    [HttpPost]
    [RequirePermission("corporate.settings.amazon_marketplaces.create")]
    public async Task<IActionResult> CreateAmazonMarketplace([FromBody] CreateAmazonMarketplaceRequest request)
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
            var marketplaceId = await _amazonMarketplaceService.CreateAsync(request);
            var successResponse = ResponseStructure<int>.Success(marketplaceId, "Amazon marketplace created successfully.");
            return Ok(successResponse);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var errorResponse = ResponseStructure<object>.Error(
                $"Error creating Amazon marketplace: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error creating Amazon marketplace: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Updates an existing Amazon marketplace by ID.
    /// </summary>
    /// <param name="amazonMarketplaceId">ID of the marketplace to update.</param>
    /// <param name="request">Updated data.</param>
    [HttpPut("{amazonMarketplaceId}")]
    [RequirePermission("corporate.settings.amazon_marketplaces.update")]
    public async Task<IActionResult> UpdateAmazonMarketplace(int amazonMarketplaceId, [FromBody] UpdateAmazonMarketplaceRequest request)
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
            var updated = await _amazonMarketplaceService.UpdateAsync(amazonMarketplaceId, request);
            if (!updated)
            {
                var notFoundResponse = ResponseStructure<object>.Error(
                    $"Amazon marketplace with ID {amazonMarketplaceId} was not found.",
                    404);
                return NotFound(notFoundResponse);
            }
            var successResponse = ResponseStructure.Success("Amazon marketplace updated successfully.");
            return Ok(successResponse);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var errorResponse = ResponseStructure<object>.Error(
                $"Error updating Amazon marketplace: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error updating Amazon marketplace: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }
}
