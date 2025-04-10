using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Specialties;
using FluentValidation;
using E_Dukate.Application.Utilities;
using E_Dukate.Domain.Entities.Specialties;

namespace E_Dukate.Application.Services.Specialties;

public class SpecialtyService : BaseService<Specialty, SpecialtyDto>
{
    private readonly IGenericRepository<Specialty> _repository;

    public SpecialtyService(IGenericRepository<Specialty> repository, IValidator<SpecialtyDto> validator)
        : base(repository, validator)
    {
        _repository = repository;
    }

    public override void Register(SpecialtyDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var normalizedSpecialty = TextNormalizer.Normalize(dto.TypeOfSpecialty);
        var specialties = _repository.GetAll().ToList();
        var existingSpecialty = specialties
            .FirstOrDefault(s => TextNormalizer.Normalize(s.TypeOfSpecialty) == normalizedSpecialty);

        if (existingSpecialty != null)
            throw new InvalidOperationException($"The specialty '{dto.TypeOfSpecialty}' already exists.");

        var specialty = new Specialty
        {
            TypeOfSpecialty = dto.TypeOfSpecialty
        };
        _repository.Add(specialty);
    }

    public override void Update(Guid id, SpecialtyDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var existing = _repository.GetById(id);
        if (existing == null)
            throw new Exception($"{typeof(Specialty).Name} not found.");

        var normalizedSpecialty = TextNormalizer.Normalize(dto.TypeOfSpecialty);
        var specialties = _repository.GetAll().ToList();
        var duplicateSpecialty = specialties
            .FirstOrDefault(s => TextNormalizer.Normalize(s.TypeOfSpecialty) == normalizedSpecialty && s.Id != id);

        if (duplicateSpecialty != null)
            throw new InvalidOperationException($"The specialty '{dto.TypeOfSpecialty}' already exists.");

        UpdateEntity(existing, dto);
        _repository.Update(existing);
    }

    protected override Specialty MapToEntity(SpecialtyDto dto)
    {
        return new Specialty
        {
            TypeOfSpecialty = dto.TypeOfSpecialty
        };
    }

    protected override void UpdateEntity(Specialty entity, SpecialtyDto dto)
    {
        entity.TypeOfSpecialty = dto.TypeOfSpecialty;
    }
}