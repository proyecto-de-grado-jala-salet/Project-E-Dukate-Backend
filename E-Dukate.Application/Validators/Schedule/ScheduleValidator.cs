using FluentValidation;
using E_Dukate.Application.DTOs.Schedules;

namespace E_Dukate.Application.Validators;

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
                .Must(ts => TimeOnly.Parse(ts.EndTime) > TimeOnly.Parse(ts.StartTime))
                .WithMessage("End time must be after start time.");
        });

        RuleFor(x => x.TimeSlots)
            .Must(BeNonOverlappingAndSequential)
            .WithMessage("Time slots must be sequential and non-overlapping. Each StartTime must be after or equal to the previous EndTime.");
    }

    private bool BeValidDayOfWeek(string dayOfWeek)
    {
        return Enum.TryParse<DayOfWeek>(dayOfWeek, true, out _);
    }

    private bool BeValidTime(string time)
    {
        return TimeOnly.TryParse(time, out _);
    }

    private bool BeNonOverlappingAndSequential(IList<TimeSlotDto> timeSlots)
    {
        if (timeSlots == null || timeSlots.Count <= 1)
            return true;

        var orderedTimeSlots = timeSlots
            .Select(ts => new { Start = TimeOnly.Parse(ts.StartTime), End = TimeOnly.Parse(ts.EndTime) })
            .OrderBy(ts => ts.Start)
            .ToList();

        for (int i = 1; i < orderedTimeSlots.Count; i++)
        {
            var previousSlot = orderedTimeSlots[i - 1];
            var currentSlot = orderedTimeSlots[i];

            if (currentSlot.Start < previousSlot.End)
            {
                return false;
            }
        }

        return true;
    }
}