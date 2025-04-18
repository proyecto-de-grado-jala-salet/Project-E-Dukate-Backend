using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;

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
        Repository.GetAll().Include(s => s.Specialty).FirstOrDefault(s => s.Id == id);

    public IEnumerable<Specialist> GetAllSpecialists() =>
        Repository.GetAll().Include(s => s.Specialty).ToList();

    protected override Specialist MapToEntity(SpecialistDto dto)
    {
        var specialty = _specialtyRepository.GetAll()
            .FirstOrDefault(s => s.TypeOfSpecialty == dto.TypeOfSpecialty)
            ?? throw new Exception("The chosen specialty does not exist");

        return new Specialist
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
    }
}