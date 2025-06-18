using FluentValidation;
using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Domain.Entities.Appointments;

namespace E_Dukate.Application.Validators.Appointments;

public class AppointmentValidator : AbstractValidator<AppointmentDto>
{
    public AppointmentValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("El ID del paciente es requerido.");

        RuleFor(x => x.SpecialtyId)
            .NotEmpty().WithMessage("El ID de la especialidad es requerido.");

        RuleFor(x => x.SpecialistId)
            .NotEmpty().WithMessage("El ID del especialista es requerido.");

        RuleFor(x => x.SessionCount)
            .GreaterThan(0).WithMessage("El número de sesiones debe ser mayor a 0.");

        RuleFor(x => x.SessionCost)
            .GreaterThan(0).WithMessage("El costo por sesión debe ser mayor a 0.");

        RuleFor(x => x.ScheduledSessions)
            .NotEmpty().WithMessage("Debe especificar al menos una sesión programada.");

        RuleForEach(x => x.ScheduledSessions).ChildRules(session =>
        {
            session.RuleFor(s => s.TimeSlotId)
                .NotEmpty().WithMessage("El ID del horario es requerido.");

            session.RuleFor(s => s.DayOfWeek)
                .NotEmpty().WithMessage("El día de la semana es requerido.")
                .Must(d => Enum.TryParse<DayOfWeek>(d, true, out _))
                .WithMessage("El día de la semana debe ser válido (Monday, Tuesday, etc.).");

            session.RuleFor(s => s.StartTime)
                .NotEmpty().WithMessage("La hora de inicio es requerida.")
                .Must(t => TimeOnly.TryParse(t, out _))
                .WithMessage("La hora de inicio debe tener un formato válido (HH:mm).");

            session.RuleFor(s => s.EndTime)
                .NotEmpty().WithMessage("La hora de fin es requerida.")
                .Must(t => TimeOnly.TryParse(t, out _))
                .WithMessage("La hora de fin debe tener un formato válido (HH:mm).");

            session.RuleFor(s => s.Status)
                .Must(s => Enum.TryParse<ScheduledSessionStatus>(s, true, out _))
                .WithMessage("El estado de la sesión debe ser 'Scheduled', 'Confirmed', 'Cancelled', o 'Rescheduled'.");
        });
    }
}