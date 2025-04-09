using E_Dukate.Application.DTOs.Specialties;
using FluentValidation;

namespace E_Dukate.Application.Validators.Specialties;

public class SpecialtyValidator : AbstractValidator<SpecialtyDto>
{
    public SpecialtyValidator()
    {
        RuleFor(x => x.TypeOfSpecialty)
            .NotEmpty().WithMessage("Type of specialty is required.")
            .Length(2, 100).WithMessage("Type of specialty must be between 2 and 100 characters.");
    }
}