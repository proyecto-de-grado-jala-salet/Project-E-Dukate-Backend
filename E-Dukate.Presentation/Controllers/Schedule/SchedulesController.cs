using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Application.DTOs.Schedules;
using E_Dukate.Domain.Entities.Schedules;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly ScheduleService _scheduleService;
    private readonly IGenericRepository<Specialist> _specialistRepository; // Añadido

    public SchedulesController(
        ScheduleService scheduleService,
        IGenericRepository<Specialist> specialistRepository) // Inyectar el repositorio
    {
        _scheduleService = scheduleService;
        _specialistRepository = specialistRepository;
    }

    [HttpPut("specialist/{specialistId}")]
    public IActionResult UpdateSchedules(Guid specialistId, [FromBody] List<ScheduleDto> scheduleDtos)
    {
        try
        {
            var result = _scheduleService.UpdateSchedules(specialistId, scheduleDtos);
            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.ErrorMessage.Split(", ").ToList() });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { "Ocurrió un error inesperado.", ex.Message } });
        }
    }

    [HttpGet("specialist/{specialistId}")]
    public IActionResult GetSchedules(Guid specialistId)
    {
        var schedules = _scheduleService.GetSchedulesBySpecialistId(specialistId);
        var specialist = _specialistRepository.GetAll()
            .FirstOrDefault(s => s.Id == specialistId);
        if (specialist == null)
            return NotFound(new { errors = new[] { "Especialista no encontrado." } });

        var response = schedules.Select(s => new
        {
            s.Id,
            DayOfWeek = s.DayOfWeek.ToString(),
            ConsultationDuration = specialist.ConsultationDuration, // Obtenido del Specialist
            s.Attends,
            TimeSlots = s.TimeSlots.Select(ts => new
            {
                ts.Id,
                StartTime = ts.StartTime.ToString("HH:mm"),
                EndTime = ts.EndTime.ToString("HH:mm")
            }).ToList()
        });
        return Ok(response);
    }
}