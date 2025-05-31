using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Users;
using FluentValidation;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Primitives;
using E_Dukate.Application.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Application.Services.Users;

public class PatientService : BaseService<Patient, PatientDto>
{
    private readonly IGenericRepository<MedicalHistory> _medicalHistoryRepository;

    public PatientService(
        IGenericRepository<Patient> repository,
        IGenericRepository<MedicalHistory> medicalHistoryRepository,
        IValidator<PatientDto> validator)
        : base(repository, validator)
    {
        _medicalHistoryRepository = medicalHistoryRepository;
    }

    public override Result Register(PatientDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var patient = MapToEntity(dto);
        Repository.Add(patient);

        var medicalHistory = new MedicalHistory
        {
            PatientId = patient.Id,
            Patient = patient
        };

        patient.MedicalHistoryId = medicalHistory.Id;
        patient.MedicalHistory = medicalHistory;
        UpdateEntity(patient, dto);
        _medicalHistoryRepository.Add(medicalHistory);

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

    public async Task<(IEnumerable<Patient> Items, int TotalCount)> SearchPatientsAsync(string searchTerm, PaginationParams pagination)
    {
        var query = Repository.GetAll();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            var searchTerms = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            query = query.Where(p =>
                (searchTerms.Length > 1
                    ? searchTerms.Any(term => p.Names.ToLower().Contains(term)) &&
                      (searchTerms.Any(term => p.LastNamePaternal.ToLower().Contains(term)) ||
                       (p.LastNameMaternal != null && searchTerms.Any(term => p.LastNameMaternal.ToLower().Contains(term))))
                    : false) ||
                p.Names.ToLower().Contains(searchTerm) ||
                p.LastNamePaternal.ToLower().Contains(searchTerm) ||
                (p.LastNameMaternal != null && p.LastNameMaternal.ToLower().Contains(searchTerm)) ||
                p.MobileNumber.Contains(searchTerm) ||
                p.IdentityCard.ToString().Contains(searchTerm) ||
                p.Age.ToString().Contains(searchTerm) ||
                p.Gender.ToLower().Contains(searchTerm)
            );
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Names)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}