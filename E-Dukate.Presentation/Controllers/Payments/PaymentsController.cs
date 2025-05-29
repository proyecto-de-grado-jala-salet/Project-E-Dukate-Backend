using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Payments;
using E_Dukate.Application.DTOs.Payments;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Application.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace E_Dukate.Presentation.Controllers.Payments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : BaseController<Payment, PaymentDto>
{
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentService service) : base(service)
    {
        _paymentService = service;
    }

    [HttpPut("{id}/pay")]
    [Authorize(Roles = "Administrator,Specialist")]
    public IActionResult UpdatePaymentAmount(Guid id, [FromBody] decimal totalAmountPaid)
    {
        var payment = _paymentService.FindById(id);
        if (payment == null)
            return NotFound("Pago no encontrado.");

        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty);
            if (payment.SpecialistId != userId)
                return Unauthorized("No tienes permiso para modificar este pago.");
        }

        var result = _paymentService.UpdatePaymentAmount(id, totalAmountPaid);
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage.Contains("no encontrado"))
                return NotFound(new { Error = result.ErrorMessage });
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return Ok(new
        {
            AmountPaid = payment.AmountPaid,
            PendingAmount = payment.PendingAmount,
            Status = payment.Status.ToString()
        });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Administrator,Specialist")]
    public override IActionResult GetById(Guid id)
    {
        var payment = _paymentService.FindById(id);
        if (payment == null)
            return NotFound();

        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty);
            if (payment.SpecialistId != userId)
                return Unauthorized("No tienes permiso para ver este pago.");
        }

        var response = new
        {
            payment.Id,
            AppointmentId = payment.AppointmentId,
            Patient = new
            {
                payment.Patient.Id,
                payment.Patient.Names,
                payment.Patient.LastNamePaternal,
                payment.Patient.LastNameMaternal
            },
            Specialist = new
            {
                payment.Specialist.Id,
                payment.Specialist.Names,
                payment.Specialist.LastNamePaternal,
                payment.Specialist.LastNameMaternal
            },
            payment.SessionCost,
            payment.SessionCount,
            payment.TotalAmount,
            payment.AmountPaid,
            payment.PendingAmount,
            payment.SpecialistAmount,
            payment.InstitutionAmount,
            payment.FirstPaymentDate,
            payment.LastPaymentDate,
            payment.Status
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

        var (items, totalCount) = await _paymentService.GetPagedAsync(pagination, specialistId);
        var response = items.Select(payment => new
        {
            payment.Id,
            AppointmentId = payment.AppointmentId,
            Patient = new
            {
                payment.Patient.Id,
                payment.Patient.Names,
                payment.Patient.LastNamePaternal,
                payment.Patient.LastNameMaternal
            },
            Specialist = new
            {
                payment.Specialist.Id,
                payment.Specialist.Names,
                payment.Specialist.LastNamePaternal,
                payment.Specialist.LastNameMaternal
            },
            payment.SessionCost,
            payment.SessionCount,
            payment.TotalAmount,
            payment.AmountPaid,
            payment.PendingAmount,
            payment.SpecialistAmount,
            payment.InstitutionAmount,
            payment.FirstPaymentDate,
            payment.LastPaymentDate,
            payment.Status
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