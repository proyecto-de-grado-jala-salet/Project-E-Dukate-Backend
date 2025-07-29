using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.MedicalHistories;

namespace E_Dukate.Application.Services.MedicalHistories;

public class MedicalHistoryService
{
    private readonly IGenericRepository<MedicalHistory> _medicalHistoryRepository;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<MedicalHistoryPermission> _permissionRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;

    public MedicalHistoryService(
        IGenericRepository<MedicalHistory> medicalHistoryRepository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<MedicalHistoryPermission> permissionRepository,
        IGenericRepository<Specialist> specialistRepository)
    {
        _medicalHistoryRepository = medicalHistoryRepository;
        _patientRepository = patientRepository;
        _permissionRepository = permissionRepository;
        _specialistRepository = specialistRepository;
    }

    public async Task<MedicalHistoryDto?> GetByPatientIdAsync(Guid patientId)
    {
        if (!await _patientRepository.GetAll().AnyAsync(p => p.Id == patientId))
            return null;

        var medicalHistory = await _medicalHistoryRepository.GetAll()
            .Include(mh => mh.Permissions)
                .ThenInclude(p => p.Consultations)
            .Include(mh => mh.Permissions)
                .ThenInclude(p => p.Documents)
            .FirstOrDefaultAsync(mh => mh.PatientId == patientId);

        if (medicalHistory == null) return null;

        return new MedicalHistoryDto
        {
            Id = medicalHistory.Id,
            PatientId = medicalHistory.PatientId,
            Permissions = medicalHistory.Permissions.Select(p => new MedicalHistoryPermissionDto
            {
                Id = p.Id,
                SpecialistId = p.SpecialistId,
                CanEdit = p.CanEdit,
                Status = p.Status.ToString(),
                Consultations = p.Consultations.Select(c => new MedicalConsultationDto
                {
                    Id = c.Id,
                    SpecialistId = c.SpecialistId,
                    Reason = c.Reason,
                    ConsultationDate = c.ConsultationDate,
                    Notes = c.Notes
                }).ToList(),
                Documents = p.Documents.Select(d => new MedicalDocumentDto
                {
                    Id = d.Id,
                    FileName = d.FileName!,
                    UploadDate = d.UploadDate
                }).ToList()
            }).ToList()
        };
    }

    public async Task<bool> UpdateEditingPermissionAsync(PermissionRequestDto request)
    {
        var medicalHistory = await _medicalHistoryRepository.GetByIdAsync(request.MedicalHistoryId);
        if (medicalHistory == null) return false;

        var specialist = await _specialistRepository.GetByIdAsync(request.SpecialistId);
        if (specialist == null) return false;

        var existingPermission = await _permissionRepository.GetAll()
            .FirstOrDefaultAsync(p =>
                p.MedicalHistoryId == request.MedicalHistoryId &&
                p.SpecialistId == request.SpecialistId);

        if (existingPermission != null)
        {
            existingPermission.CanEdit = request.CanEdit;
            await _permissionRepository.UpdateAsync(existingPermission);
        }
        else
        {
            var newPermission = new MedicalHistoryPermission
            {
                MedicalHistoryId = request.MedicalHistoryId,
                SpecialistId = request.SpecialistId,
                MedicalHistory = medicalHistory,
                Specialist = specialist,
                CanEdit = request.CanEdit,
                Status = MedicalHistoryStatus.ContinuaEnTratamiento
            };
            await _permissionRepository.AddAsync(newPermission);
        }

        return true;
    }

    public async Task<bool> UpdateMedicalHistoryStatusAsync(
        Guid medicalHistoryId,
        Guid specialistId,
        UpdateMedicalHistoryStatusDto request)
    {
        var medicalHistory = await _medicalHistoryRepository.GetByIdAsync(medicalHistoryId);
        if (medicalHistory == null) return false;

        var specialist = await _specialistRepository.GetByIdAsync(specialistId);
        if (specialist == null) return false;

        var permission = await _permissionRepository.GetAll()
            .FirstOrDefaultAsync(p =>
                p.MedicalHistoryId == medicalHistoryId &&
                p.SpecialistId == specialistId);

        if (permission == null)
            return false;

        if (!permission.CanEdit)
            return false;

        permission.Status = request.Status;
        await _permissionRepository.UpdateAsync(permission);
        return true;
    }

    public async Task<bool> DeletePermissionAsync(Guid permissionId)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId);
        if (permission == null)
            return false;

        try
        {
            await _permissionRepository.DeleteAsync(permissionId);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}