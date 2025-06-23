using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.DTOs.Metrics;
using E_Dukate.Application.Services.Metrics;

namespace E_Dukate.Presentation.Controllers.Metrics;

[ApiController]
[Route("api/metrics/demographics")]
[Authorize(Roles = "Administrator")]
public class DemographicMetricsController : ControllerBase
{
    private readonly DemographicMetricsService _metricsService;

    public DemographicMetricsController(DemographicMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetDemographicMetrics([FromQuery] DemographicFilterDto filter)
    {
        try
        {
            var metrics = await _metricsService.GetDemographicMetricsAsync(filter);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}
