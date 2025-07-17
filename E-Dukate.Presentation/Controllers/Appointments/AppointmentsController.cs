using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Appointments;
using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Application.DTOs.Common;

namespace E_Dukate.Presentation.Controllers.Appointments;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;

    public AppointmentsController(AppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
    {
        var result = await _appointmentService.CreateAppointmentAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });

        return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
    }

    [HttpPut("{id}")]
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _appointmentService.DeleteAppointmentAsync(id);
        if (!result.IsSuccess)
            return NotFound(new { Error = result.ErrorMessage });

        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null)
            return NotFound();

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
    public async Task<IActionResult> ConfirmSession(Guid id, Guid sessionId, [FromQuery] Guid patientId)
    {
        var result = await _appointmentService.ConfirmSessionAsync(sessionId, patientId);
        if (!result.IsSuccess)
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });

        return Ok();
    }

    [HttpPost("{id}/sessions/{sessionId}/cancel")]
    public async Task<IActionResult> CancelSession(Guid id, Guid sessionId, [FromQuery] Guid patientId)
    {
        var result = await _appointmentService.CancelSessionAsync(sessionId, patientId);
        if (!result.IsSuccess)
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });

        return Ok();
    }

    [HttpPost("{id}/sessions/{sessionId}/reschedule")]
    public async Task<IActionResult> RescheduleSession(Guid id, Guid sessionId, [FromQuery] Guid patientId, [FromBody] ScheduledSessionDto dto)
    {
        var result = await _appointmentService.RescheduleSessionAsync(sessionId, patientId, dto);
        if (!result.IsSuccess)
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });

        return Ok();
    }
}