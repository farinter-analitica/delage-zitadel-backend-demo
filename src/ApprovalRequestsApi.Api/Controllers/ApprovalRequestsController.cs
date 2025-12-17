using ApprovalRequestsApi.Application.DTOs.Requests;
using ApprovalRequestsApi.Application.DTOs.Responses;
using ApprovalRequestsApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApprovalRequestsApi.Api.Controllers;

[ApiController]
[Route("api/approval-requests")]
[Authorize(AuthenticationSchemes = "ZitadelJWT")]
public class ApprovalRequestsController : ControllerBase
{
    private readonly IApprovalRequestService _service;
    private readonly ILogger<ApprovalRequestsController> _logger;

    public ApprovalRequestsController(
        IApprovalRequestService service,
        ILogger<ApprovalRequestsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crear una nueva solicitud de aprobación
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(ApprovalRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApprovalRequestResponseDto>> Create(
        [FromBody] CreateApprovalRequestDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("No se pudo obtener el ID del usuario");

        var result = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Obtener las solicitudes del usuario actual
    /// </summary>
    [HttpGet("my-requests")]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(ApprovalRequestListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApprovalRequestListDto>> GetMyRequests(
        [FromQuery] SearchRequestDto searchDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("No se pudo obtener el ID del usuario");

        return Ok(await _service.GetUserRequestsAsync(userId, searchDto));
    }

    /// <summary>
    /// Obtener todas las solicitudes (solo administradores)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApprovalRequestListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApprovalRequestListDto>> GetAll(
        [FromQuery] SearchRequestDto searchDto)
    {
        return Ok(await _service.GetAllRequestsAsync(searchDto));
    }

    /// <summary>
    /// Obtener una solicitud específica por ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(ApprovalRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApprovalRequestResponseDto>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("No se pudo obtener el ID del usuario");

        var isAdmin = User.IsInRole("Admin");

        try
        {
            return Ok(await _service.GetByIdAsync(id, userId, isAdmin));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Revisar una solicitud (aprobar o rechazar) (solo administradores)
    /// </summary>
    [HttpPatch("{id}/review")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApprovalRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApprovalRequestResponseDto>> Review(
        Guid id, [FromBody] ReviewApprovalRequestDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized("No se pudo obtener el ID del administrador");

        try
        {
            var decisionDto = new ApprovalDecisionDto { AdminComments = dto.AdminComments };
            
            if (dto.Status?.ToLower() == "approved")
            {
                return Ok(await _service.ApproveAsync(id, adminId, decisionDto));
            }
            else if (dto.Status?.ToLower() == "rejected")
            {
                return Ok(await _service.RejectAsync(id, adminId, decisionDto));
            }
            else
            {
                return BadRequest(new { error = "Invalid action. Use 'Approved' or 'Rejected'" });
            }
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Aprobar una solicitud (solo administradores)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApprovalRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApprovalRequestResponseDto>> Approve(
        Guid id, [FromBody] ApprovalDecisionDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized("No se pudo obtener el ID del administrador");

        try
        {
            return Ok(await _service.ApproveAsync(id, adminId, dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Rechazar una solicitud (solo administradores)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApprovalRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApprovalRequestResponseDto>> Reject(
        Guid id, [FromBody] ApprovalDecisionDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized("No se pudo obtener el ID del administrador");

        try
        {
            return Ok(await _service.RejectAsync(id, adminId, dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
