using E_Dukate.Application.DTOs.Common;
using E_Dukate.Application.DTOs.Payments;
using E_Dukate.Application.Services.Payments;
using Microsoft.AspNetCore.Mvc;

namespace E_Dukate.Presentation.Controllers.Payments;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetFilteredPayments([FromQuery] PaymentFilterDto filter)
    {
        var (payments, totalCount) = await _paymentService.GetFilteredPaymentsAsync(filter);

        var response = new
        {
            Items = payments.Select(p => new
            {
                p.Id,
                p.AppointmentId,
                p.PatientId,
                p.SpecialistId,
                p.SessionCost,
                p.SessionCount,
                p.TotalAmount,
                p.AmountPaid,
                p.PendingAmount,
                p.SpecialistAmount,
                p.InstitutionAmount,
                p.FirstPaymentDate,
                p.LastPaymentDate,
                Status = p.Status.ToString()
            }),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        };

        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(Guid id, [FromBody] PaymentDto dto)
    {
        var result = await _paymentService.UpdatePaymentAsync(id, dto);
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage.Contains("no encontrado"))
                return NotFound(new { Error = result.ErrorMessage });
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(Guid id)
    {
        var result = await _paymentService.DeletePaymentAsync(id);
        if (!result.IsSuccess)
            return NotFound(new { Error = result.ErrorMessage });

        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPaymentById(Guid id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
            return NotFound();

        return Ok(new
        {
            payment.Id,
            payment.AppointmentId,
            payment.PatientId,
            payment.SpecialistId,
            payment.SessionCost,
            payment.SessionCount,
            payment.TotalAmount,
            payment.AmountPaid,
            payment.PendingAmount,
            payment.SpecialistAmount,
            payment.InstitutionAmount,
            payment.FirstPaymentDate,
            payment.LastPaymentDate,
            Status = payment.Status.ToString()
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] PaginationParams pagination)
    {
        var (payments, totalCount) = await _paymentService.GetPaymentsAsync(pagination);

        var response = new
        {
            Items = payments.Select(p => new
            {
                p.Id,
                p.AppointmentId,
                p.PatientId,
                p.SpecialistId,
                p.SessionCost,
                p.SessionCount,
                p.TotalAmount,
                p.AmountPaid,
                p.PendingAmount,
                p.SpecialistAmount,
                p.InstitutionAmount,
                p.FirstPaymentDate,
                p.LastPaymentDate,
                Status = p.Status.ToString()
            }),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize)
        };

        return Ok(response);
    }
}