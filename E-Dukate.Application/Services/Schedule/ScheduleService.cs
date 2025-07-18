using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Schedules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Primitives;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Application.DTOs.Common;
using E_Dukate.Domain.Entities.Appointments;

namespace E_Dukate.Application.Services;

public class ScheduleService
{
    private readonly IGenericRepository<Schedule> _scheduleRepository;
    private readonly IGenericRepository<TimeSlot> _timeSlotRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;
    private readonly IValidator<ScheduleDto> _validator;

    public ScheduleService(
        IGenericRepository<Schedule> scheduleRepository,
        IGenericRepository<TimeSlot> timeSlotRepository,
        IGenericRepository<Specialist> specialistRepository,
        IValidator<ScheduleDto> validator)
    {
        _scheduleRepository = scheduleRepository;
        _timeSlotRepository = timeSlotRepository;
        _specialistRepository = specialistRepository;
        _validator = validator;
    }

    public async Task<Result> UpdateSchedulesAsync(Guid specialistId, List<ScheduleDto> scheduleDtos)
    {
        foreach (var dto in scheduleDtos)
        {
            var validationResult = _validator.Validate(dto);
            if (!validationResult.IsValid)
                return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        var specialist = await _specialistRepository.GetByIdAsync(specialistId);
        if (specialist == null)
            return Result.Failure("Specialist not found.");

        var existingSchedules = _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == specialistId)
            .ToList();

        var scheduleIdsToDelete = existingSchedules.Select(s => s.Id).ToList();
        var timeSlotIdsToDelete = existingSchedules.SelectMany(s => s.TimeSlots.Select(ts => ts.Id)).ToList();

        try
        {
            await _timeSlotRepository.DeleteRelatedEntitiesAsync<ScheduledSession>(
    timeSlotIdsToDelete, 
    ss => timeSlotIdsToDelete.Contains(ss.TimeSlotId));

            foreach (var timeSlotId in timeSlotIdsToDelete)
            {
                var timeSlot = await _timeSlotRepository.GetByIdAsync(timeSlotId);
                if (timeSlot != null)
                {
                    await _timeSlotRepository.DeleteAsync(timeSlotId);
                }
            }

            foreach (var scheduleId in scheduleIdsToDelete)
            {
                var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
                if (schedule != null)
                {
                    await _scheduleRepository.DeleteAsync(scheduleId);
                }
            }
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Error al eliminar horarios o citas: {ex.InnerException?.Message ?? ex.Message}");
        }

        foreach (var dto in scheduleDtos)
        {
            try
            {
                var dayOfWeek = Enum.Parse<DayOfWeek>(dto.DayOfWeek!, true);
                var schedule = new Schedule
                {
                    SpecialistId = specialistId,
                    Specialist = specialist,
                    DayOfWeek = dayOfWeek,
                    Attends = dto.Attends,
                    TimeSlots = new List<TimeSlot>()
                };

                await _scheduleRepository.AddAsync(schedule);
                
                foreach (var ts in dto.TimeSlots)
                {
                    var timeSlot = new TimeSlot
                    {
                        ScheduleId = schedule.Id,
                        Schedule = schedule,
                        StartTime = TimeOnly.Parse(ts.StartTime!),
                        EndTime = TimeOnly.Parse(ts.EndTime!)
                    };
                    await _timeSlotRepository.AddAsync(timeSlot);
                    schedule.TimeSlots.Add(timeSlot);
                }
            }
            catch (ArgumentException ex)
            {
                return Result.Failure($"Datos de horario inválidos: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                return Result.Failure($"Error al guardar en la base de datos: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
        return Result.Success();
    }

    public IEnumerable<Schedule> GetSchedulesBySpecialistId(Guid specialistId) =>
        _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots.OrderByDescending(ts => ts.StartTime))
            .Where(s => s.SpecialistId == specialistId)
            .ToList();

    public async Task<(IEnumerable<Specialist>, int)> SearchSpecialistsAsync(
    string searchTerm, PaginationParams pagination)
    {
        var query = _specialistRepository.GetAll()
            .Include(s => s.Specialty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(s =>
                s.Names.ToLower().Contains(searchTerm) ||
                s.LastNamePaternal.ToLower().Contains(searchTerm) ||
                (s.LastNameMaternal != null &&
                 s.LastNameMaternal.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Names)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}