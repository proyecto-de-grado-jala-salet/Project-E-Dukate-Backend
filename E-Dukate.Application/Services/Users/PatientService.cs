using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Users;
using FluentValidation;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Application.Services.MedicalHistories;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Application.Services.Users;

public class PatientService : BaseService<Patient, PatientDto>
{
    private readonly MedicalHistoryService _medicalHistoryService;

    public PatientService(
        IGenericRepository<Patient> repository,
        IValidator<PatientDto> validator,
        MedicalHistoryService medicalHistoryService)
        : base(repository, validator)
    {
        _medicalHistoryService = medicalHistoryService;
    }

    public override Result Register(PatientDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var patient = MapToEntity(dto);
        Repository.Add(patient);
        
        var medicalHistoryResult = _medicalHistoryService.CreateForPatient(patient.Id);
        if (!medicalHistoryResult.IsSuccess)
            return medicalHistoryResult;

        return Result.Success();
    }

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