using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Specialties;
using FluentValidation;
using E_Dukate.Domain.Entities.Specialties;

namespace E_Dukate.Application.Services.Specialties;

public class SpecialtyService : BaseService<Specialty, SpecialtyDto>
{
    public SpecialtyService(IGenericRepository<Specialty> repository, IValidator<SpecialtyDto> validator)
        : base(repository, validator) { }

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