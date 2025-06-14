using FluentValidation;
using E_Dukate.Application.DTOs.Payments;

namespace E_Dukate.Application.Validators.Payments;

public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        RuleFor(x => x.AmountPaid)
            .GreaterThanOrEqualTo(0).WithMessage("El monto pagado no puede ser negativo.");
    }
}