using E_Dukate.Application.DTOs.Users;
using FluentValidation;

namespace E_Dukate.Application.Validators.Users;

public class PatientValidator : BaseUserValidator<PatientDto>
{
    public PatientValidator()
    {
        RuleFor(x => x.Age)
            .InclusiveBetween(1, 100).WithMessage("Age must be between 1 and 100.");
    }
}