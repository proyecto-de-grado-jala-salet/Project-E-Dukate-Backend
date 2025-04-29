using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Schedules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Primitives;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Application.Services;

public class ScheduleService
{
    private readonly IGenericRepository<Schedule> _scheduleRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;
    private readonly IValidator<ScheduleDto> _validator;

    public ScheduleService(
        IGenericRepository<Schedule> scheduleRepository,
        IGenericRepository<Specialist> specialistRepository,
        IValidator<ScheduleDto> validator)
    {
        _scheduleRepository = scheduleRepository;
        _specialistRepository = specialistRepository;
        _validator = validator;
    }

    public Result UpdateSchedules(Guid specialistId, List<ScheduleDto> scheduleDtos)
    {
        foreach (var dto in scheduleDtos)
        {
            var validationResult = _validator.Validate(dto);
            if (!validationResult.IsValid)
                return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        var specialist = _specialistRepository.GetAll()
            .FirstOrDefault(s => s.Id == specialistId);
        if (specialist == null)
            return Result.Failure("Specialist not found.");

        var existingSchedules = _scheduleRepository.GetAll()
            .Where(s => s.SpecialistId == specialistId)
            .ToList();

        foreach (var schedule in existingSchedules)
        {
            _scheduleRepository.Delete(schedule.Id);
        }
        
        foreach (var dto in scheduleDtos)
        {
            var dayOfWeek = Enum.Parse<DayOfWeek>(dto.DayOfWeek, true);
            var schedule = new Schedule
            {
                SpecialistId = specialistId,
                Specialist = specialist,
                DayOfWeek = dayOfWeek,
                Attends = dto.Attends,
                TimeSlots = dto.TimeSlots.Select(ts => new TimeSlot
                {
                    StartTime = TimeOnly.Parse(ts.StartTime),
                    EndTime = TimeOnly.Parse(ts.EndTime)
                }).ToList()
            };
            _scheduleRepository.Add(schedule);
        }

        return Result.Success();
    }

    public IEnumerable<Schedule> GetSchedulesBySpecialistId(Guid specialistId) =>
        _scheduleRepository.GetAll()
            .Where(s => s.SpecialistId == specialistId)
            .ToList();
}