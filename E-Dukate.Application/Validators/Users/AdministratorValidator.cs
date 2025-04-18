using E_Dukate.Application.DTOs.Users;
using FluentValidation;

namespace E_Dukate.Application.Validators.Users;

public class AdministratorValidator : BaseUserValidator<AdministratorDto>
{
    public AdministratorValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
    }
}