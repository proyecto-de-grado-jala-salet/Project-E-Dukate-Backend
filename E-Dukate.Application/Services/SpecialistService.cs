using E_Dukate.Domain.Entities;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs;
using FluentValidation;

namespace E_Dukate.Application.Services;

public class SpecialistService : GenericService<Specialist>
{
    private readonly IGenericRepository<Specialist> _repository;
    private readonly IValidator<SpecialistDto> _validator;

    public SpecialistService(IGenericRepository<Specialist> repository, IValidator<SpecialistDto> validator)
        : base(repository)
    {
        _repository = repository;
        _validator = validator;
    }

    public void Register(SpecialistDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
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
            Specialty = dto.Specialty,
            YearsOfExperience = dto.YearsOfExperience,
            SpecialistCode = dto.SpecialistCode,
            AccessCode = dto.AccessCode
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
        existing.Specialty = dto.Specialty;
        existing.YearsOfExperience = dto.YearsOfExperience;
        existing.SpecialistCode = dto.SpecialistCode;
        existing.AccessCode = dto.AccessCode;

        _repository.Update(existing);
    }
}