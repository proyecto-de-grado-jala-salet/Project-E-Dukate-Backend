using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Appointments;
using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Application.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using E_Dukate.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Presentation.Controllers.Appointments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<Appointment> _appointmentRepository;

    public AppointmentsController(
        AppointmentService appointmentService,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<Appointment> appointmentRepository)
    {
        _appointmentService = appointmentService;
        _patientRepository = patientRepository;
        _appointmentRepository = appointmentRepository;
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

    [HttpGet("patient/{patientId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAppointmentsByPatient(Guid patientId)
    {
        try
        {
            Console.WriteLine($"🔍 Buscando citas para paciente ID: {patientId}");

            // Verificar que el paciente existe
            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                Console.WriteLine($"❌ Paciente no encontrado: {patientId}");
                return NotFound(new { Error = "Paciente no encontrado" });
            }

            Console.WriteLine($"✅ Paciente encontrado: {patient.Names} {patient.LastNamePaternal}");

            // Obtener todas las citas del paciente sin filtrar por fecha
            var appointments = await _appointmentRepository.GetAll()
                .Include(a => a.Patient)
                .Include(a => a.Specialty)
                .Include(a => a.Specialist)
                .Include(a => a.ScheduledSessions)
                    .ThenInclude(ss => ss.TimeSlot)
                .Include(a => a.Payment)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.ScheduledSessions.FirstOrDefault()!.StartSessionDateTime)
                .ToListAsync();

            Console.WriteLine($"📊 Total de citas encontradas en BD: {appointments.Count}");

            var result = appointments.Select(appointment => new
            {
                appointment.Id,
                PatientId = appointment.PatientId,
                PatientName = $"{appointment.Patient.Names} {appointment.Patient.LastNamePaternal} {(appointment.Patient.LastNameMaternal ?? "")}".Trim(),
                SpecialistId = appointment.SpecialistId,
                SpecialistName = $"{appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal} {(appointment.Specialist.LastNameMaternal ?? "")}".Trim(),
                SpecialtyId = appointment.SpecialtyId,
                SpecialtyName = appointment.Specialty.TypeOfSpecialty,
                SessionCount = appointment.SessionCount,
                ScheduledSessions = appointment.ScheduledSessions.Select(ss => new
                {
                    ss.Id,
                    ss.TimeSlotId,
                    StartSessionDateTime = ss.StartSessionDateTime,
                    EndSessionDateTime = ss.EndSessionDateTime,
                    Status = ss.Status.ToString(),
                    DayOfWeek = ss.StartSessionDateTime.DayOfWeek.ToString(),
                    FormattedDate = ss.StartSessionDateTime.ToString("dd/MM/yyyy"),
                    FormattedTime = ss.StartSessionDateTime.ToString("HH:mm"),
                    IsFuture = ss.StartSessionDateTime > DateTime.UtcNow,
                    IsActive = ss.Status.ToString() == "Scheduled" || ss.Status.ToString() == "Confirmed"
                }).OrderBy(ss => ss.StartSessionDateTime).ToList(),
                PaymentStatus = appointment.Payment?.Status.ToString() ?? "Pending",
                TotalAmount = appointment.Payment?.TotalAmount ?? 0,
                AmountPaid = appointment.Payment?.AmountPaid ?? 0,
                HasActiveSessions = appointment.ScheduledSessions.Any(ss => 
                    ss.StartSessionDateTime > DateTime.UtcNow && 
                    (ss.Status.ToString() == "Scheduled" || ss.Status.ToString() == "Confirmed"))
            }).ToList();

            Console.WriteLine($"✅ Citas procesadas: {result.Count}");

            return Ok(new { 
                Items = result, 
                TotalCount = result.Count,
                Patient = new {
                    patient.Id,
                    patient.Names,
                    patient.LastNamePaternal,
                    patient.LastNameMaternal,
                    patient.IdentityCard
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error obteniendo citas del paciente: {ex.Message}");
            return StatusCode(500, new { Error = "Error interno del servidor" });
        }
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