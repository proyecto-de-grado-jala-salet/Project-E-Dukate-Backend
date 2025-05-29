using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Appointments;
using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Application.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace E_Dukate.Presentation.Controllers.Appointments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : BaseController<Appointment, AppointmentDto>
{
    private readonly AppointmentService _appointmentService;

    public AppointmentsController(AppointmentService service) : base(service)
    {
        _appointmentService = service;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Specialist")]
    public override IActionResult Add([FromBody] AppointmentDto dto)
    {
        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty);
            if (dto.SpecialistId != userId)
                return Unauthorized("No tienes permiso para crear una cita para otro especialista.");
        }

        var result = _appointmentService.Register(dto);
        if (!result.IsSuccess)
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });

        return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Specialist")]
    public override IActionResult Update(Guid id, [FromBody] AppointmentDto dto)
    {
        var appointment = _appointmentService.FindById(id);
        if (appointment == null)
            return NotFound("Cita no encontrada.");

        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty);
            if (appointment.SpecialistId != userId)
                return Unauthorized("No tienes permiso para modificar esta cita.");
        }

        var result = _appointmentService.Update(id, dto);
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage.Contains("no encontrada") || result.ErrorMessage.Contains("no encontrado"))
                return NotFound(new { Error = result.ErrorMessage });
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator,Specialist")]
    public override IActionResult Delete(Guid id)
    {
        var appointment = _appointmentService.FindById(id);
        if (appointment == null)
            return NotFound("Cita no encontrada.");

        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty);
            if (appointment.SpecialistId != userId)
                return Unauthorized("No tienes permiso para eliminar esta cita.");
        }

        try
        {
            _appointmentService.Delete(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Administrator,Specialist")]
    public override IActionResult GetById(Guid id)
    {
        var appointment = _appointmentService.FindById(id);
        if (appointment == null)
            return NotFound();

        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty);
            if (appointment.SpecialistId != userId)
                return Unauthorized("No tienes permiso para ver esta cita.");
        }

        var response = new
        {
            appointment.Id,
            Patient = new
            {
                appointment.Patient.Id,
                appointment.Patient.Names,
                appointment.Patient.LastNamePaternal,
                appointment.Patient.LastNameMaternal
            },
            Specialist = new
            {
                appointment.Specialist.Id,
                appointment.Specialist.Names,
                appointment.Specialist.LastNamePaternal,
                appointment.Specialist.LastNameMaternal
            },
            Specialty = new
            {
                appointment.Specialty.Id,
                appointment.Specialty.TypeOfSpecialty
            },
            appointment.StartTime,
            appointment.EndTime,
            appointment.SessionCount,
            appointment.Status,
            Payment = appointment.Payment != null ? new
            {
                appointment.Payment.Id,
                appointment.Payment.SessionCost,
                appointment.Payment.SessionCount,
                appointment.Payment.TotalAmount,
                appointment.Payment.AmountPaid,
                appointment.Payment.PendingAmount,
                appointment.Payment.SpecialistAmount,
                appointment.Payment.InstitutionAmount,
                appointment.Payment.FirstPaymentDate,
                appointment.Payment.LastPaymentDate,
                appointment.Payment.Status
            } : null
        };

        return Ok(response);
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,Specialist")]
    public override async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        Guid? specialistId = null;
        if (User.IsInRole("Specialist"))
        {
            specialistId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty);
        }

        var (items, totalCount) = await _appointmentService.GetPagedAsync(pagination, specialistId);
        var response = items.Select(appointment => new
        {
            appointment.Id,
            Patient = new
            {
                appointment.Patient.Id,
                appointment.Patient.Names,
                appointment.Patient.LastNamePaternal,
                appointment.Patient.LastNameMaternal
            },
            Specialist = new
            {
                appointment.Specialist.Id,
                appointment.Specialist.Names,
                appointment.Specialist.LastNamePaternal,
                appointment.Specialist.LastNameMaternal
            },
            Specialty = new
            {
                appointment.Specialty.Id,
                appointment.Specialty.TypeOfSpecialty
            },
            appointment.StartTime,
            appointment.EndTime,
            appointment.SessionCount,
            appointment.Status,
            Payment = appointment.Payment != null ? new
            {
                appointment.Payment.Id,
                appointment.Payment.SessionCost,
                appointment.Payment.SessionCount,
                appointment.Payment.TotalAmount,
                appointment.Payment.AmountPaid,
                appointment.Payment.PendingAmount,
                appointment.Payment.SpecialistAmount,
                appointment.Payment.InstitutionAmount,
                appointment.Payment.FirstPaymentDate,
                appointment.Payment.LastPaymentDate,
                appointment.Payment.Status
            } : null
        });

        return Ok(new
        {
            Items = response,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        });
    }
}