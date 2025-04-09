using E_Dukate.Domain.Entities;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Application.Services;

public class SpecialistService : GenericService<Specialist>
{
    private readonly IGenericRepository<Specialist> _repository;
    private readonly IGenericRepository<Specialty> _specialtyRepository;
    private readonly IValidator<SpecialistDto> _validator;

    public SpecialistService(
        IGenericRepository<Specialist> repository,
        IGenericRepository<Specialty> specialtyRepository,
        IValidator<SpecialistDto> validator)
        : base(repository)
    {
        _repository = repository;
        _specialtyRepository = specialtyRepository;
        _validator = validator;
    }

    // Nuevo método específico para obtener un Specialist con su Specialty
    public Specialist? GetSpecialistById(Guid id)
    {
        return _repository.GetAll().Include(s => s.Specialty).FirstOrDefault(s => s.Id == id);
    }

    // Nuevo método específico para listar Specialists con su Specialty
    public IEnumerable<Specialist> GetAllSpecialists()
    {
        return _repository.GetAll().Include(s => s.Specialty).ToList();
    }

    public void Register(SpecialistDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var specialty = _specialtyRepository.GetAll()
            .FirstOrDefault(s => s.TypeOfSpecialty == dto.TypeOfSpecialty);
        
        if (specialty == null)
        {
            throw new Exception("The chosen specialty does not exist");
        }

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
        _repository.Add(specialist);
    }

    public void Update(Guid id, SpecialistDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var specialty = _specialtyRepository.GetAll()
            .FirstOrDefault(s => s.TypeOfSpecialty == dto.TypeOfSpecialty);
        
        if (specialty == null)
        {
            throw new Exception("The chosen specialty does not exist");
        }

        var existing = _repository.GetById(id);
        if (existing == null)
        {
            throw new Exception("Specialist not found.");
        }

        existing.Names = dto.Names;
        existing.LastNamePaternal = dto.LastNamePaternal;
        existing.LastNameMaternal = dto.LastNameMaternal;
        existing.MobileNumber = dto.MobileNumber;
        existing.IdentityCard = dto.IdentityCard;
        existing.PhoneNumber = dto.PhoneNumber;
        existing.Age = dto.Age;
        existing.Gender = dto.Gender;
        existing.DateOfBirth = dto.DateOfBirth;
        existing.Address = dto.Address;
        existing.Email = dto.Email;
        existing.Password = dto.Password;
        existing.Specialty = specialty;
        existing.YearsOfExperience = dto.YearsOfExperience;
        existing.SpecialistCode = dto.SpecialistCode;

        _repository.Update(existing);
    }
}