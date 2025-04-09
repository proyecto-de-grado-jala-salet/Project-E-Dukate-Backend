using E_Dukate.Application.DTOs.Users;
using FluentValidation;

namespace E_Dukate.Application.Validators.Users;

public class PatientValidator : BaseUserValidator<PatientDto>
{
    public PatientValidator()
    {
    }
}