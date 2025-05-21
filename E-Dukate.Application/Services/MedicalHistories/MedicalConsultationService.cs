using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.MedicalHistories;
using FluentValidation;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Application.Services.MedicalHistories;

public class MedicalConsultationService : BaseService<MedicalConsultation, UpdateMedicalConsultationDto>
{
    private readonly IGenericRepository<MedicalHistory> _medicalHistoryRepository;
    private readonly IGenericRepository<MedicalHistoryPermission> _permissionRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;

    public MedicalConsultationService(
        IGenericRepository<MedicalConsultation> consultationRepository,
        IGenericRepository<MedicalHistory> medicalHistoryRepository,
        IGenericRepository<MedicalHistoryPermission> permissionRepository,
        IGenericRepository<Specialist> specialistRepository,
        IValidator<UpdateMedicalConsultationDto> validator)
        : base(consultationRepository, validator)
    {
        _medicalHistoryRepository = medicalHistoryRepository;
        _permissionRepository = permissionRepository;
        _specialistRepository = specialistRepository;
    }

    public async Task<Result> CreateMedicalConsultationAsync(
        Guid medicalHistoryId,
        Guid specialistId,
        Guid permissionId,
        UpdateMedicalConsultationDto request)
    {
        var medicalHistory = await _medicalHistoryRepository.GetByIdAsync(medicalHistoryId);
        if (medicalHistory == null)
        {
            return Result.Failure("The medical history does not exist.");
        }

        var specialist = await _specialistRepository.GetByIdAsync(specialistId);
        if (specialist == null)
        {
            return Result.Failure("The specialist does not exist.");
        }

        var permission = await _permissionRepository.GetAll()
            .Include(p => p.Consultations)
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == permissionId &&
                p.MedicalHistoryId == medicalHistoryId &&
                p.SpecialistId == specialistId);

        if (permission == null)
        {
            return Result.Failure("The permit does not exist or does not match the history and specialist.");
        }

        if (!permission.CanEdit)
        {
            return Result.Failure("The specialist does not have editing permissions.");
        }

        var validationResult = Validator.Validate(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure(errors);
        }

        var consultation = MapToEntity(request);
        consultation.PermissionId = permission.Id;
        consultation.SpecialistId = specialistId;

        try
        {
            Repository.Add(consultation);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure("Error creating consultation.");
        }
    }

    public async Task<Result> UpdateMedicalConsultationAsync(Guid consultationId, UpdateMedicalConsultationDto request)
    {
        var consultation = await Repository.GetAll()
            .Include(c => c.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == consultationId);

        if (consultation == null)
        {
            return Result.Failure("The consultation does not exist.");
        }

        if (consultation.Permission == null || !consultation.Permission.CanEdit)
        {
            return Result.Failure("There are no editing permissions for this consultation.");
        }

        var validationResult = Validator.Validate(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure(errors);
        }

        try
        {
            var result = Update(consultationId, request);
            return result;
        }
        catch (Exception)
        {
            return Result.Failure("Error updating consultation.");
        }
    }

    public async Task<Result> CanSpecialistEditConsultationAsync(Guid consultationId, Guid specialistId)
    {
        var consultation = await Repository.GetAll()
            .Include(c => c.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == consultationId);

        if (consultation == null)
        {
            return Result.Failure("The consultation does not exist.");
        }

        if (consultation.SpecialistId != specialistId)
        {
            return Result.Failure("The specialist is not assigned to this consultation.");
        }

        if (consultation.Permission == null || !consultation.Permission.CanEdit)
        {
            return Result.Failure("The specialist does not have editing permissions for this consultation.");
        }

        return Result.Success();
    }

    public async Task<Result> DeleteMedicalConsultationAsync(Guid consultationId, Guid? specialistId = null)
    {
        var consultation = await Repository.GetAll()
            .Include(c => c.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == consultationId);

        if (consultation == null)
        {
            return Result.Failure("The consultation does not exist.");
        }
        
        if (specialistId.HasValue)
        {
            if (consultation.SpecialistId != specialistId)
            {
                return Result.Failure("The specialist is not assigned to this consultation.");
            }

            if (consultation.Permission == null || !consultation.Permission.CanEdit)
            {
                return Result.Failure("The specialist does not have editing permissions for this consultation.");
            }
        }

        try
        {
            await Repository.DeleteAsync(consultationId);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure("Error deleting consultation.");
        }
    }

    protected override MedicalConsultation MapToEntity(UpdateMedicalConsultationDto dto)
    {
        return new MedicalConsultation
        {
            Reason = dto.Reason,
            ConsultationDate = dto.ConsultationDate,
            Notes = dto.Notes
        };
    }

    protected override void UpdateEntity(MedicalConsultation entity, UpdateMedicalConsultationDto dto)
    {
        entity.Reason = dto.Reason;
        entity.ConsultationDate = dto.ConsultationDate;
        entity.Notes = dto.Notes;
    }
}