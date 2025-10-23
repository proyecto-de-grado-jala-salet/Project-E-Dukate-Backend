using FluentValidation;
using E_Dukate.Application.DTOs.Users;

namespace E_Dukate.Application.Validators.Users;

public class TemporaryPatientDtoValidator : AbstractValidator<TemporaryPatientDto>
{
    public TemporaryPatientDtoValidator()
    {
        RuleFor(x => x.Names)
            .NotEmpty().WithMessage("Names are required")
            .MaximumLength(100).WithMessage("Names cannot exceed 100 characters");

        RuleFor(x => x.LastNamePaternal)
            .NotEmpty().WithMessage("Paternal last name is required")
            .MaximumLength(50).WithMessage("Paternal last name cannot exceed 50 characters");

        RuleFor(x => x.IdentityCard)
            .GreaterThan(0).WithMessage("Identity card must be a positive number");

        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required")
            .Matches(@"^\d{7,15}$").WithMessage("Mobile number must be between 7 and 15 digits");

        RuleFor(x => x.Age)
            .InclusiveBetween(1, 120).WithMessage("Age must be between 1 and 120");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required")
            .Must(g => g == "Masculino" || g == "Femenino" || g == "Otro")
            .WithMessage("Gender must be Masculino, Femenino or Otro");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Date of birth must be in the past");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters");
    }
}