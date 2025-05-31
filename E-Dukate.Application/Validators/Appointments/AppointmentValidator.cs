using FluentValidation;
using E_Dukate.Application.DTOs.Appointments;

namespace E_Dukate.Application.Validators.Appointments;

public class AppointmentValidator : AbstractValidator<AppointmentDto>
{
    public AppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.SpecialistId).NotEmpty();
        RuleFor(x => x.SpecialtyId).NotEmpty();
        RuleFor(x => x.SessionCount).GreaterThan(0);
        RuleFor(x => x.SessionCost).GreaterThan(0);
    }
}