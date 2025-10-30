using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Appointments;
using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Application.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace E_Dukate.Presentation.Controllers.Appointments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;

    public AppointmentsController(AppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }

    private bool IsAdministrator()
    {
        return User.IsInRole("Administrator");
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
    {
        var result = await _appointmentService.CreateAppointmentAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });

        return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AppointmentDto dto)
    {
        var result = await _appointmentService.UpdateAppointmentAsync(id, dto);
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage.Contains("no encontrada"))
                return NotFound(new { Error = result.ErrorMessage });
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null)
            return NotFound();
        
        if (!IsAdministrator())
        {
            var currentUserId = GetCurrentUserId();
            var dynamicAppointment = appointment as dynamic;
            Guid appointmentSpecialistId = dynamicAppointment.SpecialistId;

            if (!currentUserId.HasValue || appointmentSpecialistId != currentUserId.Value)
            {
                return Forbid("No tienes permisos para ver esta cita.");
            }
        }

        return Ok(appointment);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? specialistId,
        [FromQuery] DateTime? date,
        [FromQuery] string? status,
        [FromQuery] string? patientSearch,
        [FromQuery] PaginationParams pagination)
    {
        if (!IsAdministrator())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                specialistId = currentUserId.Value;
            }
        }

        var result = await _appointmentService.GetAppointmentsAsync(patientId, specialistId, date, status, patientSearch, pagination);
        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        var (items, totalCount) = result.Value;
        return Ok(new
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        });
    }

    [HttpPost("{id}/sessions/{sessionId}/confirm")]
    [Authorize(Roles = "Administrator,Specialist")]
    public async Task<IActionResult> ConfirmSession(Guid id, Guid sessionId)
    {
        if (!IsAdministrator())
        {
            var currentUserId = GetCurrentUserId();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            
            if (appointment != null)
            {
                var dynamicAppointment = appointment as dynamic;
                Guid appointmentSpecialistId = dynamicAppointment.SpecialistId;
                
                if (!currentUserId.HasValue || appointmentSpecialistId != currentUserId.Value)
                {
                    return Forbid("No tienes permisos para confirmar sesiones de esta cita.");
                }
            }
        }

        var result = await _appointmentService.ConfirmSessionAsync(id, sessionId);
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage.Contains("no encontrada"))
                return NotFound(new { Error = result.ErrorMessage });
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return NoContent();
    }

    [HttpPut("appointment/{appointmentId}/cancel-session/{sessionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelSession(
        Guid appointmentId,
        Guid sessionId)
    {
        if (!IsAdministrator())
        {
            var currentUserId = GetCurrentUserId();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            
            if (appointment != null)
            {
                var dynamicAppointment = appointment as dynamic;
                Guid appointmentSpecialistId = dynamicAppointment.SpecialistId;
                
                if (!currentUserId.HasValue || appointmentSpecialistId != currentUserId.Value)
                {
                    return Forbid("No tienes permisos para cancelar sesiones de esta cita.");
                }
            }
        }

        var result = await _appointmentService.CancelSessionAsync(appointmentId, sessionId);
        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return NoContent();
    }

    [HttpPut("reschedule-session/{appointmentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> RescheduleSession(
        Guid appointmentId,
        [FromBody] RescheduleSessionDto dto)
    {
        if (!IsAdministrator())
        {
            var currentUserId = GetCurrentUserId();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            
            if (appointment != null)
            {
                var dynamicAppointment = appointment as dynamic;
                Guid appointmentSpecialistId = dynamicAppointment.SpecialistId;
                
                if (!currentUserId.HasValue || appointmentSpecialistId != currentUserId.Value)
                {
                    return Forbid("No tienes permisos para reprogramar sesiones de esta cita.");
                }
            }
        }

        var result = await _appointmentService.RescheduleSessionAsync(appointmentId, dto);
        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return NoContent();
    }

    [HttpPost("preview")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAppointmentPreview([FromBody] AppointmentDto dto)
    {
        var result = await _appointmentService.GetAppointmentPreviewAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        var previewData = result.Value!.Select(item => new
        {
            start = item.Start.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            end = item.End.ToString("yyyy-MM-ddTHH:mm:ssZ")
        }).ToList();

        return Ok(previewData);
    }
}