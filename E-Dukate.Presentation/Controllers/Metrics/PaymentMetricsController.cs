using E_Dukate.Application.Services.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace E_Dukate.Presentation.Controllers.Metrics;

[ApiController]
[Route("api/metrics/payments")]
public class PaymentMetricsController : ControllerBase
{
    private readonly PaymentMetricsService _paymentMetricsService;

    public PaymentMetricsController(PaymentMetricsService paymentMetricsService)
    {
        _paymentMetricsService = paymentMetricsService;
    }

    [HttpGet("total-income")]
    public async Task<IActionResult> GetTotalIncomeByPeriod(
        [FromQuery] string? periodType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                return BadRequest(new { Error = "La fecha de inicio debe ser anterior o igual a la fecha de fin." });

            var result = await _paymentMetricsService.GetTotalIncomeByPeriodAsync(periodType, startDate, endDate);
            if (!result.Any())
            {
                return Ok(new { Message = "No hay datos disponibles para el período seleccionado." });
            }
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Error al cargar los datos. Por favor, intenta de nuevo." });
        }
    }

    [HttpGet("pending-vs-completed")]
    public async Task<IActionResult> GetPendingVsCompletedPayments()
    {
        try
        {
            var result = await _paymentMetricsService.GetPendingVsCompletedPaymentsAsync();
            if (result.PendingAmount == 0 && result.CompletedAmount == 0)
            {
                return Ok(new { Message = "No hay pagos registrados." });
            }
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Error al cargar los datos. Por favor, intenta de nuevo." });
        }
    }

    [HttpGet("status-counts")]
    public async Task<IActionResult> GetPaymentStatusCounts(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                return BadRequest(new { Error = "La fecha de inicio debe ser anterior o igual a la fecha de fin." });

            var result = await _paymentMetricsService.GetPaymentStatusCountsAsync(startDate, endDate);
            if (result.PendingCount == 0 && result.CompletedCount == 0)
            {
                return Ok(new { Message = "No hay pagos registrados en el período seleccionado." });
            }
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Error al cargar los datos. Por favor, intenta de nuevo." });
        }
    }

    [HttpGet("institution-earnings")]
    public async Task<IActionResult> GetInstitutionEarnings(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                return BadRequest(new { Error = "La fecha de inicio debe ser anterior o igual a la fecha de fin." });

            var result = await _paymentMetricsService.GetInstitutionEarningsAsync(startDate, endDate);
            if (result.TotalInstitutionEarnings == 0)
            {
                return Ok(new { Message = "No hay ganancias registradas para la institución en el período seleccionado." });
            }
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Error al cargar los datos. Por favor, intenta de nuevo." });
        }
    }

    [HttpGet("available-years")]
    public async Task<ActionResult<List<int>>> GetAvailableYears()
    {
        var years = await _paymentMetricsService.GetAvailableYearsAsync();
        return Ok(years);
    }
}