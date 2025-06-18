using FluentValidation;
using E_Dukate.Application.DTOs.Payments;

namespace E_Dukate.Application.Validators.Payments;

public class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        RuleFor(x => x.AmountPaid)
            .GreaterThanOrEqualTo(0).WithMessage("El monto pagado debe ser mayor o igual a 0.");

        RuleFor(x => x.SessionCost)
            .GreaterThan(0).WithMessage("El costo por sesión debe ser mayor a 0.");
    }
}