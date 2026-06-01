using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Leads (CustomerSubmissions) desde Corporate.
/// Permite leer, crear manualmente, editar notas y marcar como leídos los leads.
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class LeadController : ControllerBase
{
    private readonly LeadService _leadService;
    private readonly ValidationService _validationService;

    public LeadController(
        LeadService leadService,
        ValidationService validationService)
    {
        _leadService = leadService;
        _validationService = validationService;
    }

    #region GET – List leads

    /// <summary>
    /// Obtiene leads con filtros opcionales. Use brandName para cargar contactos de una empresa.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLeads(
        [FromQuery] bool? isRead = null,
        [FromQuery] string? submissionType = null,
        [FromQuery] string? platform = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? brandName = null,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortOrder = null)
    {
        try
        {
            var filter = BuildFilter(isRead, submissionType, platform, search, fromDate, toDate, brandName, sortField, sortOrder);

            var leads = await _leadService.GetAllLeadsAsync(filter);
            return Ok(ResponseStructure<object>.Success(leads, "Leads obtenidos exitosamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error($"Error al obtener leads: {ex.Message}"));
        }
    }

    /// <summary>
    /// Obtiene marcas (BrandName) agrupadas y paginadas para el listado de Leads en Corporate.
    /// </summary>
    [HttpGet("brands")]
    public async Task<IActionResult> GetLeadBrandGroups(
        [FromQuery] bool? isRead = null,
        [FromQuery] string? submissionType = null,
        [FromQuery] string? platform = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortOrder = null)
    {
        try
        {
            var filter = BuildFilter(isRead, submissionType, platform, search, fromDate, toDate, brandName: null, sortField, sortOrder);
            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;

            var result = await _leadService.GetBrandGroupsAsync(filter);
            return Ok(ResponseStructure<object>.Success(result, "Marcas obtenidas exitosamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error($"Error al obtener marcas: {ex.Message}"));
        }
    }

    #endregion

    #region GET – Single lead

    /// <summary>
    /// Obtiene un lead por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLeadById(int id)
    {
        try
        {
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null)
                return NotFound(ResponseStructure<object>.NotFound($"Lead con ID {id} no encontrado"));

            return Ok(ResponseStructure<object>.Success(lead, "Lead obtenido exitosamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error($"Error al obtener el lead: {ex.Message}"));
        }
    }

    #endregion

    #region GET – Unread count

    /// <summary>
    /// Obtiene el número de leads no leídos.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var count = await _leadService.GetUnreadCountAsync();
            return Ok(ResponseStructure<object>.Success(new { count }, "Conteo obtenido exitosamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error($"Error al obtener el conteo: {ex.Message}"));
        }
    }

    #endregion

    #region POST – Create lead (manual desde Corporate)

    /// <summary>
    /// Crea un lead manualmente desde Corporate.
    /// SubmissionType se fija automáticamente como "corporate_manual".
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadRequest request)
    {
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            var userId = GetCurrentUserId();
            request.CreatedByUserId = userId;

            var id = await _leadService.CreateLeadAsync(request);
            return Ok(ResponseStructure<object>.Success(new { submissionId = id }, "Lead creado exitosamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error($"Error al crear el lead: {ex.Message}"));
        }
    }

    #endregion

    #region PUT – Update lead

    /// <summary>
    /// Actualiza los datos de un lead existente.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateLead(int id, [FromBody] UpdateLeadRequest request)
    {
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));

        try
        {
            var updated = await _leadService.UpdateLeadAsync(id, request);
            if (!updated)
                return NotFound(ResponseStructure<object>.NotFound($"Lead con ID {id} no encontrado"));

            return Ok(ResponseStructure<object>.Success(null, "Lead actualizado exitosamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error($"Error al actualizar el lead: {ex.Message}"));
        }
    }

    #endregion

    #region PATCH – Mark as read

    /// <summary>
    /// Marca un lead como leído registrando quién lo leyó y cuándo.
    /// </summary>
    [HttpPatch("{id:int}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetCurrentUserId() ?? 0;
            var updated = await _leadService.MarkAsReadAsync(id, userId);
            if (!updated)
                return NotFound(ResponseStructure<object>.NotFound($"Lead con ID {id} no encontrado"));

            return Ok(ResponseStructure<object>.Success(null, "Lead marcado como leído"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error($"Error al marcar como leído: {ex.Message}"));
        }
    }

    #endregion

    #region PATCH – Update notes

    /// <summary>
    /// Actualiza las notas de un lead.
    /// </summary>
    [HttpPatch("{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes(int id, [FromBody] UpdateLeadNotesRequest request)
    {
        try
        {
            var updated = await _leadService.UpdateNotesAsync(id, request.Notes);
            if (!updated)
                return NotFound(ResponseStructure<object>.NotFound($"Lead con ID {id} no encontrado"));

            return Ok(ResponseStructure<object>.Success(null, "Notas actualizadas exitosamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error($"Error al actualizar notas: {ex.Message}"));
        }
    }

    #endregion

    #region Private helpers

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private static LeadFilter BuildFilter(
        bool? isRead,
        string? submissionType,
        string? platform,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        string? brandName,
        string? sortField,
        string? sortOrder) =>
        new()
        {
            IsRead = isRead,
            SubmissionType = submissionType,
            Platform = platform,
            Search = search,
            FromDate = fromDate,
            ToDate = toDate,
            BrandName = brandName,
            SortField = sortField,
            SortOrder = sortOrder,
        };

    #endregion
}

public class UpdateLeadNotesRequest
{
    public string? Notes { get; set; }
}
