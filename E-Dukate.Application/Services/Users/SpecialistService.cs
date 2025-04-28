using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Schedules;

namespace E_Dukate.Application.Services.Users;

public class SpecialistService : BaseService<Specialist, SpecialistDto>
{
    private readonly IGenericRepository<Specialty> _specialtyRepository;

    public SpecialistService(
        IGenericRepository<Specialist> repository,
        IGenericRepository<Specialty> specialtyRepository,
        IValidator<SpecialistDto> validator)
        : base(repository, validator)
    {
        _specialtyRepository = specialtyRepository;
    }

    public Specialist? GetSpecialistById(Guid id) =>
        Repository.GetAll()
            .Include(s => s.Specialty)
            .Include(s => s.Schedules)
            .FirstOrDefault(s => s.Id == id);

    public IEnumerable<Specialist> GetAllSpecialists() =>
        Repository.GetAll()
            .Include(s => s.Specialty)
            .Include(s => s.Schedules)
            .ToList();

    protected override Specialist MapToEntity(SpecialistDto dto)
    {
        var specialty = _specialtyRepository.GetAll()
            .FirstOrDefault(s => s.TypeOfSpecialty == dto.TypeOfSpecialty)
            ?? throw new Exception("The chosen specialty does not exist");

        var specialist = new Specialist
        {
            Names = dto.Names,
            LastNamePaternal = dto.LastNamePaternal,
            LastNameMaternal = dto.LastNameMaternal,
            MobileNumber = dto.MobileNumber,
            IdentityCard = dto.IdentityCard,
            PhoneNumber = dto.PhoneNumber,
            Age = dto.Age,
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            Address = dto.Address,
            Email = dto.Email,
            Password = dto.Password,
            Specialty = specialty,
            YearsOfExperience = dto.YearsOfExperience,
            SpecialistCode = dto.SpecialistCode
        };

        foreach (var scheduleDto in dto.Schedules)
        {
            var dayOfWeek = Enum.Parse<DayOfWeek>(scheduleDto.DayOfWeek, true);
            var schedule = new Schedule
            {
                Specialist = specialist,
                DayOfWeek = dayOfWeek,
                Attends = scheduleDto.Attends,
                TimeSlots = scheduleDto.TimeSlots.Select(ts => new TimeSlot
                {
                    StartTime = TimeOnly.Parse(ts.StartTime),
                    EndTime = TimeOnly.Parse(ts.EndTime)
                }).ToList()
            };
            specialist.Schedules.Add(schedule);
        }

        return specialist;
    }

    protected override void UpdateEntity(Specialist entity, SpecialistDto dto)
    {
        var specialty = _specialtyRepository.GetAll()
            .FirstOrDefault(s => s.TypeOfSpecialty == dto.TypeOfSpecialty)
            ?? throw new Exception("The chosen specialty does not exist");

        entity.Names = dto.Names;
        entity.LastNamePaternal = dto.LastNamePaternal;
        entity.LastNameMaternal = dto.LastNameMaternal;
        entity.MobileNumber = dto.MobileNumber;
        entity.IdentityCard = dto.IdentityCard;
        entity.PhoneNumber = dto.PhoneNumber;
        entity.Age = dto.Age;
        entity.Gender = dto.Gender;
        entity.DateOfBirth = dto.DateOfBirth;
        entity.Address = dto.Address;
        entity.Email = dto.Email;
        entity.Password = dto.Password;
        entity.Specialty = specialty;
        entity.YearsOfExperience = dto.YearsOfExperience;
        entity.SpecialistCode = dto.SpecialistCode;
        
        entity.Schedules.Clear();
        foreach (var scheduleDto in dto.Schedules)
        {
            var dayOfWeek = Enum.Parse<DayOfWeek>(scheduleDto.DayOfWeek, true);
            var schedule = new Schedule
            {
                Specialist = entity,
                DayOfWeek = dayOfWeek,
                Attends = scheduleDto.Attends,
                TimeSlots = scheduleDto.TimeSlots.Select(ts => new TimeSlot
                {
                    StartTime = TimeOnly.Parse(ts.StartTime),
                    EndTime = TimeOnly.Parse(ts.EndTime)
                }).ToList()
            };
            entity.Schedules.Add(schedule);
        }
    }
}