using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Users;
using FluentValidation;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Application.Services.Users;

public class PatientService : BaseService<Patient, PatientDto>
{
    public PatientService(IGenericRepository<Patient> repository, IValidator<PatientDto> validator)
        : base(repository, validator) { }

    protected override Patient MapToEntity(PatientDto dto)
    {
        return new Patient
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
    }

    protected override void UpdateEntity(Patient entity, PatientDto dto)
    {
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
    }
}