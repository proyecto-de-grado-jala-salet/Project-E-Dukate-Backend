using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Infrastructure.Services.TemporaryAppointment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Dukate.Presentation.Controllers.Appointments;

[ApiController]
[Route("api/[controller]")]
public class TemporaryAppointmentsController : ControllerBase
{
    private readonly TemporaryAppointmentService _temporaryAppointmentService;

    public TemporaryAppointmentsController(TemporaryAppointmentService temporaryAppointmentService)
    {
        _temporaryAppointmentService = temporaryAppointmentService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateTemporaryAppointment([FromBody] CreateTemporaryAppointmentRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.WhatsAppNumber))
                return BadRequest(new { Error = "Número de WhatsApp es requerido" });

            var id = await _temporaryAppointmentService.CreateTemporaryAppointmentAsync(request);
            return Ok(new { Id = id, Message = "Cita temporal creada exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTemporaryAppointment(Guid id)
    {
        try
        {
            var appointment = await _temporaryAppointmentService.GetTemporaryAppointmentAsync(id);
            if (appointment == null)
                return NotFound(new { Error = "Cita temporal no encontrada" });

            return Ok(appointment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("upload-comprobante")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadComprobante([FromForm] UploadComprobanteRequestDto request)
    {
        try
        {
            var result = await _temporaryAppointmentService.UploadComprobanteAsync(request);
            if (!result.IsSuccess)
                return BadRequest(new { Error = result.ErrorMessage });

            return Ok(new { Message = "Comprobante subido exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetPendingAppointments()
    {
        try
        {
            var appointments = await _temporaryAppointmentService.GetPendingAppointmentsAsync();
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("approved")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetApprovedAppointments()
    {
        try
        {
            var appointments = await _temporaryAppointmentService.GetApprovedAppointmentsAsync();
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
    
    [HttpGet("rejected")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetRejectedAppointments()
    {
        try
        {
            var appointments = await _temporaryAppointmentService.GetRejectedAppointmentsAsync();
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{id}/verify")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> VerifyAppointment(Guid id, [FromBody] VerifyTemporaryAppointmentRequestDto request)
    {
        try
        {
            var result = await _temporaryAppointmentService.VerifyAppointmentAsync(id, request);
            if (!result.IsSuccess)
                return BadRequest(new { Error = result.ErrorMessage });

            var statusMessage = request.IsApproved ? "aprobada" : "rechazada";
            return Ok(new { Message = $"Cita {statusMessage} exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("cleanup-expired")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CleanupExpiredAppointments()
    {
        try
        {
            await _temporaryAppointmentService.CleanupExpiredAppointmentsAsync();
            return Ok(new { Message = "Limpieza de citas expiradas completada" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}