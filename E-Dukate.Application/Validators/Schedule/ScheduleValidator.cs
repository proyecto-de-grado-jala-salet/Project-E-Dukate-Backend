using FluentValidation;
using E_Dukate.Application.DTOs.Schedules;

namespace E_Dukate.Application.Validators.Schedule;

public class ScheduleValidator : AbstractValidator<ScheduleDto>
{
    public ScheduleValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .NotEmpty().WithMessage("Day of week is required.")
            .Must(BeValidDayOfWeek).WithMessage("Invalid day of week.");

        RuleForEach(x => x.TimeSlots).ChildRules(timeSlot =>
        {
            timeSlot.RuleFor(ts => ts.StartTime)
                .NotEmpty().WithMessage("Start time is required.")
                .Must(BeValidTime).WithMessage("Invalid start time format (use HH:mm).");

            timeSlot.RuleFor(ts => ts.EndTime)
                .NotEmpty().WithMessage("End time is required.")
                .Must(BeValidTime).WithMessage("Invalid end time format (use HH:mm).");

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
            .WithMessage("Time slots must be sequential and non-overlapping. Each StartTime must be after or equal to the previous EndTime.");
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