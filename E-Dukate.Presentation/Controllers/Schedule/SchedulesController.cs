using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Application.DTOs.Schedules;

namespace E_Dukate.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly ScheduleService _scheduleService;

    public SchedulesController(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
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
            return StatusCode(500, new { errors = new[] { "An unexpected error occurred.", ex.Message } });
        }
    }

    [HttpGet("specialist/{specialistId}")]
    public IActionResult GetSchedules(Guid specialistId)
    {
        var schedules = _scheduleService.GetSchedulesBySpecialistId(specialistId);
        return Ok(schedules);
    }
}