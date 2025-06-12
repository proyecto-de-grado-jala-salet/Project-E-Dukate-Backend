using FluentValidation;
using E_Dukate.Application.DTOs.FAQ;

namespace E_Dukate.Application.Validators.FAQ;

public class FaqValidator : AbstractValidator<FaqDto>
{
    public FaqValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("La pregunta es requerida.")
            .MaximumLength(500).WithMessage("La pregunta no puede exceder 500 caracteres.");

        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("La respuesta es requerida.")
            .MaximumLength(1000).WithMessage("La respuesta no puede exceder 1000 caracteres.");
    }
}