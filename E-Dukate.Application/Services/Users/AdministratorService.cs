using E_Dukate.Domain.Interfaces;
using FluentValidation;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Application.Services.Users;

public class AdministratorService : BaseService<Administrator, AdministratorDto>
{
    public AdministratorService(IGenericRepository<Administrator> repository, IValidator<AdministratorDto> validator)
        : base(repository, validator) { }

    protected override Administrator MapToEntity(AdministratorDto dto)
    {
        return new Administrator
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
            Password = dto.Password
        };
    }

    protected override void UpdateEntity(Administrator entity, AdministratorDto dto)
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
        entity.Email = dto.Email;
        entity.Password = dto.Password;
    }
}