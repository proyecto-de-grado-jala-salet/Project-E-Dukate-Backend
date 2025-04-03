using E_Dukate.Domain.Entities;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs;
using FluentValidation;

namespace E_Dukate.Application.Services;

public class AdministratorService : GenericService<Administrator>
{
    private readonly IGenericRepository<Administrator> _repository;
    private readonly IValidator<AdministratorDto> _validator;

    public AdministratorService(IGenericRepository<Administrator> repository, IValidator<AdministratorDto> validator)
        : base(repository)
    {
        _repository = repository;
        _validator = validator;
    }

    public void Register(AdministratorDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        var administrator = new Administrator
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
            AccessCode = dto.AccessCode
        };
        _repository.Add(administrator);
    }

    public void Update(Guid id, AdministratorDto dto)
    {
        // Validar el DTO
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verificar si el administrador existe
        var existing = _repository.GetById(id);
        if (existing == null)
        {
            throw new Exception("Administrator not found.");
        }

        // Actualizar los campos del administrador existente
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
        existing.Password = dto.Password; // Considera hashear si cambias la contraseña
        existing.AccessCode = dto.AccessCode;

        _repository.Update(existing);
    }
}