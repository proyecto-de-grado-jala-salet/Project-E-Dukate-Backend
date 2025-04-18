using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Specialties;
using FluentValidation;
using E_Dukate.Application.Utilities;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Application.Services.Specialties;

public class SpecialtyService : BaseService<Specialty, SpecialtyDto>
{
    private readonly IGenericRepository<Specialty> _repository;

    public SpecialtyService(IGenericRepository<Specialty> repository, IValidator<SpecialtyDto> validator)
        : base(repository, validator)
    {
        _repository = repository;
    }

    public Result Register(SpecialtyDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var normalizedSpecialty = TextNormalizer.Normalize(dto.TypeOfSpecialty);
        var specialties = _repository.GetAll().ToList();
        var existingSpecialty = specialties
            .FirstOrDefault(s => TextNormalizer.Normalize(s.TypeOfSpecialty) == normalizedSpecialty);

        if (existingSpecialty != null)
            return Result.Failure($"La especialidad '{dto.TypeOfSpecialty}' ya existe.");

        var specialty = new Specialty
        {
            TypeOfSpecialty = dto.TypeOfSpecialty
        };
        _repository.Add(specialty);
        return Result.Success();
    }

    public Result Update(Guid id, SpecialtyDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var existing = _repository.GetById(id);
        if (existing == null)
            return Result.Failure($"{typeof(Specialty).Name} not found.");

        var normalizedSpecialty = TextNormalizer.Normalize(dto.TypeOfSpecialty);
        var specialties = _repository.GetAll().ToList();
        var duplicateSpecialty = specialties
            .FirstOrDefault(s => TextNormalizer.Normalize(s.TypeOfSpecialty) == normalizedSpecialty && s.Id != id);

        if (duplicateSpecialty != null)
            return Result.Failure($"La especialidad '{dto.TypeOfSpecialty}' ya existe.");

        UpdateEntity(existing, dto);
        _repository.Update(existing);
        return Result.Success();
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