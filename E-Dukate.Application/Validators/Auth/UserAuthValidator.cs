using E_Dukate.Application.DTOs.Auth;
using FluentValidation;
using FluentValidation.Results;

namespace E_Dukate.Application.Validators.Auth;
public class UserAuthValidator
{
    private readonly IValidator<LoginDto> _validator;

    public UserAuthValidator(IValidator<LoginDto> validator)
    {
        _validator = validator;
    }

    public ValidationResult Validate(LoginDto loginDto)
    {
        return _validator.Validate(loginDto);
    }
}
