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
        // Validar cada ScheduleDto
        foreach (var dto in scheduleDtos)
        {
            var validationResult = _validator.Validate(dto);
            if (!validationResult.IsValid)
                return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        // Obtener el especialista
        var specialist = _specialistRepository.GetAll()
            .FirstOrDefault(s => s.Id == specialistId);
        if (specialist == null)
            return Result.Failure("Especialista no encontrado.");

        // Validar que ConsultationDuration coincide con el del especialista
        foreach (var dto in scheduleDtos)
        {
            if (dto.ConsultationDuration != specialist.ConsultationDuration)
            {
                return Result.Failure($"La duración de la consulta debe ser {specialist.ConsultationDuration} minutos.");
            }
        }

        // Eliminar horarios existentes
        var existingSchedules = _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == specialistId)
            .ToList();

        foreach (var schedule in existingSchedules)
        {
            _scheduleRepository.Delete(schedule.Id);
        }

        // Crear nuevos horarios
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
                    EndTime = TimeOnly.Parse(ts.EndTime),
                    ScheduleId = Guid.NewGuid() // Temporal, se actualizará después
                }).ToList()
            };
            _scheduleRepository.Add(schedule);

            // Actualizar ScheduleId en TimeSlots
            foreach (var timeSlot in schedule.TimeSlots)
            {
                timeSlot.ScheduleId = schedule.Id;
            }
        }

        return Result.Success();
    }

    public IEnumerable<Schedule> GetSchedulesBySpecialistId(Guid specialistId) =>
        _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == specialistId)
            .ToList();
}