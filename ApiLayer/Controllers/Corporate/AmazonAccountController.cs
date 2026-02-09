using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controller for Amazon Accounts maintenance (GET with optional filters, POST, PUT).
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class AmazonAccountController : ControllerBase
{
    private readonly AmazonAccountService _amazonAccountService;
    private readonly ValidationService _validationService;

    public AmazonAccountController(
        AmazonAccountService amazonAccountService,
        ValidationService validationService)
    {
        _amazonAccountService = amazonAccountService;
        _validationService = validationService;
    }

    /// <summary>
    /// Gets Amazon accounts with optional query parameters. When multiple parameters are sent, filters are combined with AND.
    /// String parameters use partial match (LIKE). When no parameters are sent, all accounts are returned.
    /// </summary>
    /// <param name="id">Optional. Return only the account with this ID.</param>
    /// <param name="customerId">Optional. Filter by customer ID (exact match).</param>
    /// <param name="amazonAccountIdentifier">Optional. Partial match on account identifier.</param>
    /// <param name="isSeller">Optional. Filter by seller flag.</param>
    /// <param name="isVendor">Optional. Filter by vendor flag.</param>
    /// <param name="amazonRegion">Optional. Partial match on Amazon region.</param>
    /// <param name="isActive">Optional. Filter by active status.</param>
    [HttpGet]
    public async Task<IActionResult> GetAmazonAccounts(
        [FromQuery] int? id = null,
        [FromQuery] int? customerId = null,
        [FromQuery] string? amazonAccountIdentifier = null,
        [FromQuery] bool? isSeller = null,
        [FromQuery] bool? isVendor = null,
        [FromQuery] string? amazonRegion = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _amazonAccountService.GetAmazonAccountsAsync(
                id, customerId, amazonAccountIdentifier, isSeller, isVendor, amazonRegion, isActive);

            if (id.HasValue && !items.Any())
            {
                var notFoundResponse = ResponseStructure<object>.Error(
                    $"Amazon account with ID {id.Value} was not found.",
                    404);
                return NotFound(notFoundResponse);
            }

            var message = items.Count() == 1 && id.HasValue
                ? "Amazon account retrieved successfully."
                : $"Found {items.Count()} Amazon account(s).";
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.AmazonAccount>>.Success(items, message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error retrieving Amazon accounts: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Creates a new Amazon account. CustomerId must exist.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAmazonAccount([FromBody] CreateAmazonAccountRequest request)
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
            var accountId = await _amazonAccountService.CreateAsync(request);
            var successResponse = ResponseStructure<int>.Success(accountId, "Amazon account created successfully.");
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
                $"Error creating Amazon account: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error creating Amazon account: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Updates an existing Amazon account by ID. CustomerId must exist.
    /// </summary>
    [HttpPut("{amazonAccountId}")]
    public async Task<IActionResult> UpdateAmazonAccount(int amazonAccountId, [FromBody] UpdateAmazonAccountRequest request)
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
            var updated = await _amazonAccountService.UpdateAsync(amazonAccountId, request);
            if (!updated)
            {
                var notFoundResponse = ResponseStructure<object>.Error(
                    $"Amazon account with ID {amazonAccountId} was not found.",
                    404);
                return NotFound(notFoundResponse);
            }
            var successResponse = ResponseStructure.Success("Amazon account updated successfully.");
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
                $"Error updating Amazon account: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error updating Amazon account: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }
}
