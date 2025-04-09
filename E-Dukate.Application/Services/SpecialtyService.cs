using E_Dukate.Domain.Entities;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs;
using FluentValidation;

namespace E_Dukate.Application.Services;

public class SpecialtyService : GenericService<Specialty>
{
    private readonly IGenericRepository<Specialty> _repository;
    private readonly IValidator<SpecialtyDto> _validator;

    public SpecialtyService(IGenericRepository<Specialty> repository, IValidator<SpecialtyDto> validator)
        : base(repository)
    {
        _repository = repository;
        _validator = validator;
    }

    public void Register(SpecialtyDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var specialty = new Specialty
        {
            TypeOfSpecialty = dto.TypeOfSpecialty
        };
        _repository.Add(specialty);
    }

    public void Update(Guid id, SpecialtyDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var existing = _repository.GetById(id);
        if (existing == null)
        {
            throw new Exception("Specialty not found.");
        }

        existing.TypeOfSpecialty = dto.TypeOfSpecialty;
        _repository.Update(existing);
    }
}