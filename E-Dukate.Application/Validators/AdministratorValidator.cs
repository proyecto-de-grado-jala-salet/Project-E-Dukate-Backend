using FluentValidation;
using E_Dukate.Application.DTOs;

namespace E_Dukate.Application.Validators;

public class AdministratorValidator : AbstractValidator<AdministratorDto>
{
    public AdministratorValidator()
    {
        RuleFor(x => x.Names)
            .NotEmpty().WithMessage("Names are required.")
            .Length(2, 50).WithMessage("Names must be between 2 and 50 characters.");

        RuleFor(x => x.LastNamePaternal)
            .NotEmpty().WithMessage("LastNamePaternal is required.")
            .Length(2, 50).WithMessage("LastNamePaternal must be between 2 and 50 characters.");

        RuleFor(x => x.LastNameMaternal)
            .NotEmpty().WithMessage("LastNameMaternal is required.")
            .Length(2, 50).WithMessage("LastNameMaternal must be between 2 and 50 characters.");

        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("MobileNumber is required.")
            .Matches("^[0-9]{8,15}$").WithMessage("MobileNumber must be 8-15 digits.");

        RuleFor(x => x.IdentityCard)
            .GreaterThan(0).WithMessage("IdentityCard must be a positive number.")
            .Must(id => id.ToString().Length == 8).WithMessage("IdentityCard must be exactly 8 digits.");

        RuleFor(x => x.PhoneNumber)
            .Matches("^[0-9]{8,15}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("PhoneNumber must be 8-15 digits if provided.");

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 100).WithMessage("Age must be between 18 and 100.");

        RuleFor(x => x.Gender)
            .Must(g => g == "M" || g == "F").WithMessage("Gender must be 'M' or 'F'.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth is required.")
            .Must(BeConsistentWithAge).WithMessage("DateOfBirth is not consistent with Age.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.AccessCode)
            .Equal("123").WithMessage("Invalid access code for Administrator.");
    }

    private bool BeConsistentWithAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age >= 18 && age <= 100;
    }
}