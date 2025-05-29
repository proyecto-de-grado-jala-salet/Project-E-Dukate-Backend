using FluentValidation;
using E_Dukate.Application.DTOs.Appointments;

namespace E_Dukate.Application.Validators.Appointments;

public class AppointmentValidator : AbstractValidator<AppointmentDto>
{
    public AppointmentValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("El ID del paciente es requerido.");

        RuleFor(x => x.SpecialistId)
            .NotEmpty().WithMessage("El ID del especialista es requerido.");

        RuleFor(x => x.SpecialtyId)
            .NotEmpty().WithMessage("El ID de la especialidad es requerido.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("La fecha de inicio es requerida.")
            .GreaterThanOrEqualTo(DateTime.UtcNow).WithMessage("La fecha de inicio debe ser futura.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("La fecha de fin es requerida.")
            .GreaterThan(x => x.StartTime).WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.SessionCount)
            .GreaterThan(0).WithMessage("El número de sesiones debe ser mayor a 0.");

        RuleFor(x => x.SessionCost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo por sesión debe ser mayor o igual a 0.");
    }
}