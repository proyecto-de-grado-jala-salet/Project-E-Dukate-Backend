using E_Dukate.Domain.Entities;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs;
using FluentValidation;

namespace E_Dukate.Application.Services;

public class PatientService : GenericService<Patient>
{
    private readonly IGenericRepository<Patient> _repository;
    private readonly IValidator<PatientDto> _validator;

    public PatientService(IGenericRepository<Patient> repository, IValidator<PatientDto> validator)
        : base(repository)
    {
        _repository = repository;
        _validator = validator;
    }

    public void Register(PatientDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            var errorMessage = validationResult.Errors.First().ErrorMessage;
            throw new ValidationException(errorMessage); // Solo el mensaje
        }

        //_validator.ValidateAndThrow(dto);
        var patient = new Patient
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
            Address = dto.Address
        };
        _repository.Add(patient);
    }

    public void Update(Guid id, PatientDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            var errorMessage = validationResult.Errors.First().ErrorMessage;
            throw new ValidationException(errorMessage);
        }

        var existing = _repository.GetById(id);
        if (existing == null)
        {
            throw new Exception("Patient not found.");
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

        _repository.Update(existing);
    }
}