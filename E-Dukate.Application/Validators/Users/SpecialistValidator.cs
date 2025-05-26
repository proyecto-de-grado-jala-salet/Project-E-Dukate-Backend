using E_Dukate.Application.DTOs.Users;
using FluentValidation;

namespace E_Dukate.Application.Validators.Users;

public class SpecialistValidator : BaseUserValidator<SpecialistDto>
{
    public SpecialistValidator()
    {
        RuleFor(x => x.Age)
            .InclusiveBetween(18, 100).WithMessage("Age must be between 18 and 100.");
            
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.TypeOfSpecialty)
            .NotEmpty().WithMessage("Specialty is required.")
            .Length(2, 100).WithMessage("Specialty must be between 2 and 100 characters.");

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be a positive number.");

        RuleFor(x => x.SpecialistCode)
            .NotEmpty().WithMessage("Specialist code is required.");
    }
}