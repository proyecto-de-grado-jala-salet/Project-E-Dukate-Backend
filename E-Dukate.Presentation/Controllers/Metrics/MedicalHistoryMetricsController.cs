using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.DTOs.Metrics;
using E_Dukate.Application.Services.Metrics;

namespace E_Dukate.Presentation.Controllers.Metrics;

[ApiController]
[Route("api/metrics/medical-histories")]
[Authorize(Roles = "Administrator")]
public class MedicalHistoryMetricsController : ControllerBase
{
    private readonly MedicalHistoryMetricsService _metricsService;

    public MedicalHistoryMetricsController(MedicalHistoryMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetMedicalHistoryMetrics([FromQuery] MedicalHistoryFilterDto filter)
    {
        try
        {
            var metrics = await _metricsService.GetMetricsAsync(filter);
            return Ok(metrics);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}