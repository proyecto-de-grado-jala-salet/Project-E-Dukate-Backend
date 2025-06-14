using FluentValidation;
using E_Dukate.Application.DTOs.Schedules;

namespace E_Dukate.Application.Validators.Schedule;

public class ScheduleValidator : AbstractValidator<ScheduleDto>
{
    public ScheduleValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .NotEmpty().WithMessage("El día de la semana es obligatorio.")
            .Must(BeValidDayOfWeek).WithMessage("Día de la semana inválido.");

        RuleFor(x => x.ConsultationDuration)
            .GreaterThan(0).WithMessage("La duración de la consulta debe ser mayor a 0 minutos.");

        RuleForEach(x => x.TimeSlots).ChildRules(timeSlot =>
        {
            timeSlot.RuleFor(ts => ts.StartTime)
                .NotEmpty().WithMessage("La hora de inicio es obligatoria.")
                .Must(BeValidTime).WithMessage("Formato de hora de inicio inválido (use HH:mm).");

            timeSlot.RuleFor(ts => ts.EndTime)
                .NotEmpty().WithMessage("La hora de fin es obligatoria.")
                .Must(BeValidTime).WithMessage("Formato de hora de fin inválido (use HH:mm).");

            timeSlot.RuleFor(ts => ts)
                .Must(ts =>
                    TimeOnly.TryParse(ts.EndTime, out var endTime) &&
                    TimeOnly.TryParse(ts.StartTime, out var startTime) &&
                    endTime > startTime
                )
                .WithMessage("End time must be after start time."); 
        });

        RuleFor(x => x.TimeSlots)
            .Must(BeNonOverlappingAndSequential)
            .WithMessage("Los intervalos de tiempo deben ser secuenciales y no superponerse. Cada StartTime debe ser igual o posterior al EndTime anterior.");
    }

    private bool BeValidDayOfWeek(string? dayOfWeek)
    {
        return !string.IsNullOrEmpty(dayOfWeek) &&
               Enum.TryParse<DayOfWeek>(dayOfWeek, true, out _);
    }

    private bool BeValidTime(string? time)
    {
        return !string.IsNullOrEmpty(time) &&
               TimeOnly.TryParse(time, out _);
    }

    private bool BeValidDuration(ScheduleDto dto, TimeSlotDto timeSlot)
    {
        if (!TimeOnly.TryParse(timeSlot.StartTime, out var startTime) ||
            !TimeOnly.TryParse(timeSlot.EndTime, out var endTime))
        {
            return false;
        }

        var duration = endTime - startTime;
        return duration.TotalMinutes == dto.ConsultationDuration;
    }

    private bool BeNonOverlappingAndSequential(IList<TimeSlotDto> timeSlots)
    {
        if (timeSlots == null || timeSlots.Count <= 1)
            return true;

        var parsedSlots = new List<(TimeOnly Start, TimeOnly End)>();
        foreach (var ts in timeSlots)
        {
            if (!TimeOnly.TryParse(ts.StartTime, out var start) ||
                !TimeOnly.TryParse(ts.EndTime, out var end))
            {
                return false;
            }
            parsedSlots.Add((start, end));
        }

        var orderedSlots = parsedSlots.OrderBy(ts => ts.Start).ToList();
        for (int i = 1; i < orderedSlots.Count; i++)
        {
            if (orderedSlots[i].Start < orderedSlots[i - 1].End)
                return false;
        }

        return true;
    }
}