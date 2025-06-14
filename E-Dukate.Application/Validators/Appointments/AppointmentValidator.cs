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

        RuleFor(x => x.ScheduledSessions)
            .NotEmpty().WithMessage("Debe especificar al menos una sesión programada.")
            .Must((dto, sessions) => sessions.Count <= dto.SessionCount)
            .WithMessage("El número de sesiones programadas no puede exceder el número total de sesiones.");

        RuleForEach(x => x.ScheduledSessions).ChildRules(session =>
        {
            session.RuleFor(s => s.TimeSlotId)
                .NotEmpty().WithMessage("El ID del horario es requerido.");

            session.RuleFor(s => s.SessionDateTime)
                .NotEmpty().WithMessage("La fecha y hora de la sesión son requeridas.");

            session.RuleFor(s => s.Status)
                .Must(s => Enum.TryParse<ScheduledSessionStatus>(s, true, out _))
                .WithMessage("El estado de la sesión debe ser 'Scheduled', 'Confirmed', 'Cancelled', o 'Rescheduled'.");
        });
    }
}