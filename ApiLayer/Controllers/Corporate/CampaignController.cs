using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiLayer.Controllers.Corporate;

[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class CampaignController : ControllerBase
{
    private readonly CampaignService _campaignService;
    private readonly ValidationService _validationService;
    private readonly IErrorLogService _errorLogService;

    public CampaignController(
        CampaignService campaignService,
        ValidationService validationService,
        IErrorLogService errorLogService)
    {
        _campaignService = campaignService;
        _validationService = validationService;
        _errorLogService = errorLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCampaigns([FromQuery] string? status = null)
    {
        try
        {
            var campaigns = await _campaignService.GetAllAsync(status);
            return Ok(ResponseStructure<object>.Success(campaigns, "Campaigns retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { status }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving campaigns. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpGet("{campaignId:int}")]
    public async Task<IActionResult> GetCampaignById(int campaignId)
    {
        try
        {
            var campaign = await _campaignService.GetByIdAsync(campaignId);
            if (campaign == null)
                return NotFound(ResponseStructure<object>.NotFound($"Campaign with ID {campaignId} not found."));

            return Ok(ResponseStructure<object>.Success(campaign, "Campaign retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { campaignId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving campaign. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(string.Join(", ", validationResult.Errors)));

        try
        {
            var campaignId = await _campaignService.CreateAsync(request);
            return Ok(ResponseStructure<object>.Success(new { campaignId }, "Campaign created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while creating campaign. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpPut("{campaignId:int}")]
    public async Task<IActionResult> UpdateCampaign(int campaignId, [FromBody] CreateCampaignRequest request)
    {
        var updateRequest = new UpdateCampaignRequest
        {
            CampaignId = campaignId,
            Name = request.Name,
            Subject = request.Subject,
            BodyContent = request.BodyContent,
            ScheduledAt = request.ScheduledAt,
            CreatedByUserId = request.CreatedByUserId
        };

        var validationResult = await _validationService.ValidateAsync(updateRequest);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(string.Join(", ", validationResult.Errors)));

        try
        {
            await _campaignService.UpdateAsync(updateRequest);
            return Ok(ResponseStructure<object>.Success(null, "Campaign updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(updateRequest));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while updating campaign. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpGet("{campaignId:int}/attachments")]
    public async Task<IActionResult> GetAttachments(int campaignId)
    {
        try
        {
            var attachments = await _campaignService.GetAttachmentsAsync(campaignId);
            return Ok(ResponseStructure<object>.Success(attachments, "Attachments retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { campaignId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving attachments. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpPost("{campaignId:int}/attachments")]
    public async Task<IActionResult> UploadAttachments(int campaignId, [FromForm] UploadCampaignAttachmentsRequest request)
    {
        request.CampaignId = campaignId;

        try
        {
            var count = await _campaignService.UploadAttachmentsAsync(request);
            return Ok(ResponseStructure<object>.Success(new { attachmentCount = count }, "Attachments uploaded successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { campaignId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while uploading attachments. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpGet("{campaignId:int}/prospects")]
    public async Task<IActionResult> GetProspects(int campaignId)
    {
        try
        {
            var prospects = await _campaignService.GetProspectsAsync(campaignId);
            return Ok(ResponseStructure<object>.Success(prospects, "Prospects retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { campaignId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving prospects. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpPost("{campaignId:int}/prospects")]
    public async Task<IActionResult> AddProspects(int campaignId, [FromBody] AddCampaignProspectsRequest request)
    {
        request.CampaignId = campaignId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(string.Join(", ", validationResult.Errors)));

        try
        {
            var result = await _campaignService.AddProspectsAsync(request);
            return Ok(ResponseStructure<object>.Success(result, "Prospects processed successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while adding prospects. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpPost("{campaignId:int}/prospects/from-leads")]
    public async Task<IActionResult> AddProspectsFromLeads(int campaignId, [FromBody] AddCampaignProspectsFromLeadsRequest request)
    {
        request.CampaignId = campaignId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(string.Join(", ", validationResult.Errors)));

        try
        {
            var result = await _campaignService.AddProspectsFromLeadsAsync(request);
            return Ok(ResponseStructure<object>.Success(result, "Leads added as prospects successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while adding leads as prospects. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    [HttpDelete("{campaignId:int}/prospects/{prospectId:int}")]
    public async Task<IActionResult> DeleteProspect(int campaignId, int prospectId)
    {
        try
        {
            await _campaignService.DeleteProspectAsync(campaignId, prospectId);
            return Ok(ResponseStructure<object>.Success(null, "Prospect removed successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { campaignId, prospectId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while removing prospect. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    /// <summary>
    /// Sends the campaign. Each recipient receives an individual BCC email (recipients never see each other).
    /// </summary>
    [HttpPost("{campaignId:int}/send")]
    public async Task<IActionResult> SendCampaign(int campaignId, [FromBody] SendCampaignRequest request)
    {
        request.CampaignId = campaignId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(string.Join(", ", validationResult.Errors)));

        try
        {
            var result = await _campaignService.SendAsync(request);
            return Ok(ResponseStructure<object>.Success(result, result.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while sending campaign. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }
}
