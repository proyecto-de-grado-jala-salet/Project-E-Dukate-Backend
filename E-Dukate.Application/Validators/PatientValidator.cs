using FluentValidation;
using E_Dukate.Application.DTOs;

namespace E_Dukate.Application.Validators;

public class PatientValidator : AbstractValidator<PatientDto>
{
    public PatientValidator()
    {
        RuleFor(x => x.Names)
            .NotEmpty().WithMessage("Names required.")
            .Length(2, 50).WithMessage("Names must be between 2 and 50 characters.");

        RuleFor(x => x.LastNamePaternal)
            .NotEmpty().WithMessage("Paternal last name is required.")
            .Length(2, 50).WithMessage("Paternal Last Name must be between 2 and 50 characters.");

        RuleFor(x => x.LastNameMaternal)
            .NotEmpty().WithMessage("Mother's last name is mandatory.")
            .Length(2, 50).WithMessage("Maternal Last Name must be between 2 and 50 characters.");

        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("Cell Phone Number is required.")
            .Matches("^[67][0-9]{7}$").WithMessage("Cell Phone Number must begin with the number '6' or '7' and must be 8 digits.");

        RuleFor(x => x.IdentityCard)
            .GreaterThan(0).WithMessage("The National Identity Card must be a positive number.")
            .Must(id => id.ToString().Length is 7 or 8).WithMessage("The National Identity Card must be between 7 and 8 digits.");

        RuleFor(x => x.PhoneNumber)
            .Matches("^[0-9]{8}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Telephone Number must be 8 digits.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => g == "M" || g == "F").WithMessage("Gender must be 'M' or 'F'.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of Birth is required.")
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("The Date of Birth must not exceed the current date.")
            .Must(date => date >= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-100)))
            .WithMessage("The date of birth entered must not be older than 100 years from the current date.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(200).WithMessage("The address must not exceed 200 characters.");
    }
}
