using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Services;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controller for managing Opportunities in Corporate.
/// Handles lead conversion, opportunity listing, stages, follow-ups, and comments.
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class OpportunityController : ControllerBase
{
    private readonly OpportunityService _opportunityService;
    private readonly ValidationService _validationService;
    private readonly IErrorLogService _errorLogService;
    private readonly OpportunitySignalRService _signalRService;

    public OpportunityController(
        OpportunityService opportunityService,
        ValidationService validationService,
        IErrorLogService errorLogService,
        OpportunitySignalRService signalRService)
    {
        _opportunityService = opportunityService;
        _validationService = validationService;
        _errorLogService = errorLogService;
        _signalRService = signalRService;
    }

    #region GET - List opportunities

    /// <summary>
    /// Gets all opportunities with optional filters.
    /// </summary>
    /// <param name="status">Filter by status: Open, Won, Lost</param>
    /// <param name="stageKey">Filter by current stage key</param>
    /// <param name="assignedToUserId">Filter by assigned user ID</param>
    /// <param name="viewerUserId">Filter by viewer user ID</param>
    /// <param name="mine">Show only my opportunities (where I'm assigned or viewer)</param>
    /// <param name="search">Search in name, email, brand</param>
    /// <param name="fromDate">Filter from date (converted at)</param>
    /// <param name="toDate">Filter to date (converted at)</param>
    [HttpGet]
    public async Task<IActionResult> GetOpportunities(
        [FromQuery] string? status = null,
        [FromQuery] string? stageKey = null,
        [FromQuery] int? assignedToUserId = null,
        [FromQuery] int? viewerUserId = null,
        [FromQuery] bool? mine = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var filter = new OpportunityFilter
            {
                Status = status,
                CurrentStageKey = stageKey,
                AssignedToUserId = assignedToUserId,
                ViewerUserId = viewerUserId,
                MyOpportunities = mine,
                UserId = currentUserId,
                Search = search,
                FromDate = fromDate,
                ToDate = toDate
            };

            var opportunities = await _opportunityService.GetAllOpportunitiesAsync(filter);
            return Ok(ResponseStructure<object>.Success(opportunities, "Opportunities retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new
            {
                status, stageKey, assignedToUserId, viewerUserId, mine, search, fromDate, toDate
            }));

            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving opportunities. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Single opportunity

    /// <summary>
    /// Gets an opportunity by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOpportunityById(int id)
    {
        try
        {
            var opportunity = await _opportunityService.GetOpportunityByIdAsync(id);
            
            if (opportunity == null)
                return NotFound(ResponseStructure<object>.NotFound($"Opportunity with ID {id} not found."));

            return Ok(ResponseStructure<object>.Success(opportunity, "Opportunity retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { opportunityId = id }));

            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving opportunity {id}. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Count by status

    /// <summary>
    /// Gets count of opportunities by status.
    /// </summary>
    [HttpGet("count/{status}")]
    public async Task<IActionResult> GetCountByStatus(string status)
    {
        try
        {
            var count = await _opportunityService.GetCountByStatusAsync(status);
            return Ok(ResponseStructure<object>.Success(new { count }, "Count retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { status }));

            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while counting opportunities. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region POST - Convert lead to opportunity

    /// <summary>
    /// Converts a lead to an opportunity.
    /// </summary>
    [HttpPost("convert-from-lead")]
    public async Task<IActionResult> ConvertLeadToOpportunity([FromBody] ConvertLeadToOpportunityRequest request)
    {
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            var currentUserId = GetCurrentUserId();
            request.ConvertedByUserId = currentUserId;

            var opportunityId = await _opportunityService.ConvertLeadToOpportunityAsync(request);
            
            return Ok(ResponseStructure<object>.Success(
                new { opportunityId }, 
                "Lead converted to opportunity successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));

            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while converting lead to opportunity. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Stages catalog

    /// <summary>
    /// Gets all active opportunity stages.
    /// </summary>
    [HttpGet("stages")]
    public async Task<IActionResult> GetStages()
    {
        try
        {
            var stages = await _opportunityService.GetAllActiveStagesAsync();
            return Ok(ResponseStructure<object>.Success(stages, "Stages retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext);
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving stages. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Lost reasons catalog

    /// <summary>
    /// Gets all active lost reasons.
    /// </summary>
    [HttpGet("lost-reasons")]
    public async Task<IActionResult> GetLostReasons()
    {
        try
        {
            var reasons = await _opportunityService.GetAllActiveLostReasonsAsync();
            return Ok(ResponseStructure<object>.Success(reasons, "Lost reasons retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext);
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving lost reasons. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Follow-ups by opportunity

    /// <summary>
    /// Gets all follow-ups for an opportunity.
    /// </summary>
    [HttpGet("{opportunityId:int}/follow-ups")]
    public async Task<IActionResult> GetFollowUpsByOpportunity(int opportunityId)
    {
        try
        {
            var followUps = await _opportunityService.GetFollowUpsByOpportunityIdAsync(opportunityId);
            return Ok(ResponseStructure<object>.Success(followUps, "Follow-ups retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { opportunityId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving follow-ups. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Follow-up by ID

    /// <summary>
    /// Gets a follow-up by ID.
    /// </summary>
    [HttpGet("follow-ups/{followUpId:int}")]
    public async Task<IActionResult> GetFollowUpById(int followUpId)
    {
        try
        {
            var followUp = await _opportunityService.GetFollowUpByIdAsync(followUpId);
            
            if (followUp == null)
                return NotFound(ResponseStructure<object>.NotFound($"Follow-up with ID {followUpId} not found."));

            return Ok(ResponseStructure<object>.Success(followUp, "Follow-up retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { followUpId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving follow-up. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Pending follow-ups (dashboard alerts)

    /// <summary>
    /// Gets pending follow-ups for dashboard alerts.
    /// If userId is provided, filters by assigned or viewer user.
    /// </summary>
    [HttpGet("follow-ups/pending")]
    public async Task<IActionResult> GetPendingFollowUps([FromQuery] int? userId = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            // If no userId param, use current user
            var filterUserId = userId ?? currentUserId;

            var pendingFollowUps = await _opportunityService.GetPendingFollowUpsAsync(filterUserId);
            return Ok(ResponseStructure<object>.Success(pendingFollowUps, "Pending follow-ups retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { userId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving pending follow-ups. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Follow-up attachments

    /// <summary>
    /// Gets attachments for a follow-up.
    /// </summary>
    [HttpGet("follow-ups/{followUpId:int}/attachments")]
    public async Task<IActionResult> GetFollowUpAttachments(int followUpId)
    {
        try
        {
            var attachments = await _opportunityService.GetFollowUpAttachmentsAsync(followUpId);
            return Ok(ResponseStructure<object>.Success(attachments, "Attachments retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { followUpId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving attachments. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region POST - Create follow-up

    /// <summary>
    /// Creates a manual follow-up for an opportunity.
    /// </summary>
    [HttpPost("{opportunityId:int}/follow-ups")]
    public async Task<IActionResult> CreateFollowUp(int opportunityId, [FromBody] CreateFollowUpRequest request)
    {
        request.OpportunityId = opportunityId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            var followUpId = await _opportunityService.CreateFollowUpAsync(request);
            return Ok(ResponseStructure<object>.Success(
                new { followUpId }, 
                "Follow-up created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while creating follow-up. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region POST - Complete follow-up with attachments

    /// <summary>
    /// Completes a follow-up and uploads evidence attachments to Azure Blob Storage.
    /// </summary>
    [HttpPost("follow-ups/{followUpId:int}/complete")]
    public async Task<IActionResult> CompleteFollowUp(int followUpId, [FromForm] CompleteFollowUpRequest request)
    {
        request.FollowUpId = followUpId;

        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(ResponseStructure<object>.Error("User not authenticated."));

            request.CompletedByUserId = currentUserId.Value;

            var response = await _opportunityService.CompleteFollowUpAsync(request);
            return Ok(ResponseStructure<object>.Success(response, response.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new
            {
                followUpId,
                notes = request.Notes,
                fileCount = request.Files?.Count ?? 0
            }));

            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while completing follow-up. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region POST - Change opportunity stage

    /// <summary>
    /// Changes the current stage of an opportunity.
    /// </summary>
    [HttpPost("{opportunityId:int}/change-stage")]
    public async Task<IActionResult> ChangeOpportunityStage(int opportunityId, [FromBody] ChangeOpportunityStageRequest request)
    {
        request.OpportunityId = opportunityId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            await _opportunityService.ChangeOpportunityStageAsync(request);
            return Ok(ResponseStructure<object>.Success(null, "Opportunity stage changed successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while changing opportunity stage. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region POST - Mark opportunity as won

    /// <summary>
    /// Marks an opportunity as won and links it to a customer and optionally a contract.
    /// </summary>
    [HttpPost("{opportunityId:int}/mark-as-won")]
    public async Task<IActionResult> MarkOpportunityAsWon(int opportunityId, [FromBody] MarkOpportunityAsWonRequest request)
    {
        request.OpportunityId = opportunityId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(ResponseStructure<object>.Error("User not authenticated."));

            request.WonByUserId = currentUserId.Value;

            await _opportunityService.MarkOpportunityAsWonAsync(request);
            return Ok(ResponseStructure<object>.Success(null, "Opportunity marked as won successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while marking opportunity as won. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region POST - Mark opportunity as lost

    /// <summary>
    /// Marks an opportunity as lost with a reason.
    /// </summary>
    [HttpPost("{opportunityId:int}/mark-as-lost")]
    public async Task<IActionResult> MarkOpportunityAsLost(int opportunityId, [FromBody] MarkOpportunityAsLostRequest request)
    {
        request.OpportunityId = opportunityId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(ResponseStructure<object>.Error("User not authenticated."));

            request.LostByUserId = currentUserId.Value;

            await _opportunityService.MarkOpportunityAsLostAsync(request);
            return Ok(ResponseStructure<object>.Success(null, "Opportunity marked as lost successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while marking opportunity as lost. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Comments by opportunity

    /// <summary>
    /// Gets all comments for an opportunity.
    /// </summary>
    [HttpGet("{opportunityId:int}/comments")]
    public async Task<IActionResult> GetCommentsByOpportunity(int opportunityId)
    {
        try
        {
            var comments = await _opportunityService.GetCommentsByOpportunityIdAsync(opportunityId);
            return Ok(ResponseStructure<object>.Success(comments, "Comments retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { opportunityId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving comments. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Comment by ID

    /// <summary>
    /// Gets a comment by ID.
    /// </summary>
    [HttpGet("comments/{commentId:int}")]
    public async Task<IActionResult> GetCommentById(int commentId)
    {
        try
        {
            var comment = await _opportunityService.GetCommentByIdAsync(commentId);
            
            if (comment == null)
                return NotFound(ResponseStructure<object>.NotFound($"Comment with ID {commentId} not found."));

            return Ok(ResponseStructure<object>.Success(comment, "Comment retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { commentId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving comment. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Comment attachments

    /// <summary>
    /// Gets attachments for a comment.
    /// </summary>
    [HttpGet("comments/{commentId:int}/attachments")]
    public async Task<IActionResult> GetCommentAttachments(int commentId)
    {
        try
        {
            var attachments = await _opportunityService.GetCommentAttachmentsAsync(commentId);
            return Ok(ResponseStructure<object>.Success(attachments, "Attachments retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { commentId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving attachments. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region GET - Comment mentions

    /// <summary>
    /// Gets mentions for a comment.
    /// </summary>
    [HttpGet("comments/{commentId:int}/mentions")]
    public async Task<IActionResult> GetCommentMentions(int commentId)
    {
        try
        {
            var mentions = await _opportunityService.GetCommentMentionsAsync(commentId);
            return Ok(ResponseStructure<object>.Success(mentions, "Mentions retrieved successfully."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { commentId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while retrieving mentions. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region POST - Create comment

    /// <summary>
    /// Creates a comment on an opportunity with optional attachments.
    /// Supports @mentions (e.g., @123) which will trigger email notifications.
    /// </summary>
    [HttpPost("{opportunityId:int}/comments")]
    public async Task<IActionResult> CreateComment(int opportunityId, [FromForm] CreateOpportunityCommentRequest request)
    {
        request.OpportunityId = opportunityId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(ResponseStructure<object>.Error("User not authenticated."));

            request.AuthorUserId = currentUserId.Value;

            var response = await _opportunityService.CreateCommentAsync(request);

            var authorName = User.FindFirst("FullName")?.Value
                ?? User.Identity?.Name
                ?? $"User {currentUserId.Value}";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _signalRService.NotifyCommentCreatedAsync(
                        opportunityId,
                        response.CommentId,
                        currentUserId.Value,
                        authorName,
                        request.Body,
                        response.MentionedUserIds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send SignalR notification: {ex.Message}");
                }
            });

            return Ok(ResponseStructure<object>.Success(response, response.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new
            {
                opportunityId,
                body = request.Body,
                fileCount = request.Files?.Count ?? 0
            }));

            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while creating comment. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region PUT - Update comment

    /// <summary>
    /// Updates a comment (only author can update).
    /// </summary>
    [HttpPut("comments/{commentId:int}")]
    public async Task<IActionResult> UpdateComment(int commentId, [FromBody] UpdateOpportunityCommentRequest request)
    {
        request.CommentId = commentId;

        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(ResponseStructure<object>.Error("User not authenticated."));

            request.AuthorUserId = currentUserId.Value;

            await _opportunityService.UpdateCommentAsync(request);
            return Ok(ResponseStructure<object>.Success(null, "Comment updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(request));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while updating comment. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region DELETE - Delete comment

    /// <summary>
    /// Deletes a comment (only author can delete).
    /// </summary>
    [HttpDelete("comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(ResponseStructure<object>.Error("User not authenticated."));

            await _opportunityService.DeleteCommentAsync(commentId, currentUserId.Value);
            return Ok(ResponseStructure<object>.Success(null, "Comment deleted successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, JsonSerializer.Serialize(new { commentId }));
            return StatusCode(500, ResponseStructure<object>.Error(
                $"An error occurred while deleting comment. Error ID: {errorNumber}. Message: {ex.Message}"));
        }
    }

    #endregion

    #region Private helpers

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    #endregion
}
