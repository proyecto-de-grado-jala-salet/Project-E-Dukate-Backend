using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.MedicalHistories;
using FluentValidation;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Application.Services.MedicalHistories;

public class MedicalHistoryService : BaseService<MedicalHistory, MedicalHistoryDto>
{
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<MedicalHistory> _medicalHistoryRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;

    public MedicalHistoryService(
        IGenericRepository<MedicalHistory> repository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<Specialist> specialistRepository,
        IValidator<MedicalHistoryDto> validator)
        : base(repository, validator)
    {
        _patientRepository = patientRepository;
        _medicalHistoryRepository = repository;
        _specialistRepository = specialistRepository;
    }

    public Result CreateForPatient(Guid patientId)
    {
        var patient = _patientRepository.GetById(patientId);
        if (patient == null)
            return Result.Failure("Paciente no encontrado.");

        var existingHistory = Repository.GetAll().FirstOrDefault(mh => mh.PatientId == patientId);
        if (existingHistory != null)
            return Result.Failure("El paciente ya tiene un historial médico.");

        var medicalHistory = new MedicalHistory
        {
            PatientId = patientId,
            Patient = patient
        };

        Repository.Add(medicalHistory);
        return Result.Success();
    }

    public async Task<ValueResult<MedicalHistoryDto>> GetByPatientIdAsync(Guid patientId)
    {
        var medicalHistory = await _medicalHistoryRepository.GetAll()
            .Include(mh => mh.Permissions)
            .ThenInclude(p => p.Consultations)
            .FirstOrDefaultAsync(mh => mh.PatientId == patientId);

        if (medicalHistory == null)
            return ValueResult<MedicalHistoryDto>.Failure("Historial médico no encontrado para el paciente.");

        var response = new MedicalHistoryDto
        {
            PatientId = medicalHistory.PatientId,
            MedicalHistoryPermissions = medicalHistory.Permissions.Select(p => new MedicalHistoryPermissionDto
            {
                SpecialistId = p.SpecialistId,
                CanEdit = p.CanEdit,
                Status = p.Status,
                MedicalConsultations = p.Consultations.Select(c => new MedicalConsultationDto
                {
                    SpecialistId = c.SpecialistId,
                    Reason = c.Reason,
                    ConsultationDate = c.ConsultationDate,
                    Notes = c.Notes
                }).ToList()
            }).ToList()
        };

        return ValueResult<MedicalHistoryDto>.Success(response);
    }

    protected override MedicalHistory MapToEntity(MedicalHistoryDto dto)
    {
        var patient = _patientRepository.GetById(dto.PatientId)
            ?? throw new Exception("Paciente no encontrado.");

        var medicalHistory = new MedicalHistory
        {
            PatientId = dto.PatientId,
            Patient = patient
        };

        medicalHistory.Permissions = dto.MedicalHistoryPermissions.Select(p =>
        {
            var specialist = _specialistRepository.GetById(p.SpecialistId)
                ?? throw new Exception($"Especialista con ID {p.SpecialistId} no encontrado.");

            var permission = new MedicalHistoryPermission
            {
                MedicalHistory = medicalHistory,
                MedicalHistoryId = medicalHistory.Id,
                SpecialistId = p.SpecialistId,
                Specialist = specialist,
                CanEdit = p.CanEdit,
                Status = p.Status
            };

            permission.Consultations = p.MedicalConsultations.Select(c =>
            {
                var consultationSpecialist = _specialistRepository.GetById(c.SpecialistId)
                    ?? throw new Exception($"Especialista con ID {c.SpecialistId} no encontrado.");

                return new MedicalConsultation
                {
                    MedicalHistory = medicalHistory,
                    MedicalHistoryId = medicalHistory.Id,
                    SpecialistId = c.SpecialistId,
                    Specialist = consultationSpecialist,
                    Reason = c.Reason,
                    ConsultationDate = c.ConsultationDate,
                    Notes = c.Notes
                };
            }).ToList();

            return permission;
        }).ToList();

        return medicalHistory;
    }

    protected override void UpdateEntity(MedicalHistory entity, MedicalHistoryDto dto)
    {
        var patient = _patientRepository.GetById(dto.PatientId)
            ?? throw new Exception("Paciente no encontrado.");

        entity.PatientId = dto.PatientId;
        entity.Patient = patient;

        entity.Permissions = dto.MedicalHistoryPermissions.Select(p =>
        {
            var specialist = _specialistRepository.GetById(p.SpecialistId)
                ?? throw new Exception($"Especialista con ID {p.SpecialistId} no encontrado.");

            var permission = new MedicalHistoryPermission
            {
                MedicalHistory = entity,
                MedicalHistoryId = entity.Id,
                SpecialistId = p.SpecialistId,
                Specialist = specialist,
                CanEdit = p.CanEdit,
                Status = p.Status
            };

            permission.Consultations = p.MedicalConsultations.Select(c =>
            {
                var consultationSpecialist = _specialistRepository.GetById(c.SpecialistId)
                    ?? throw new Exception($"Especialista con ID {c.SpecialistId} no encontrado.");

                return new MedicalConsultation
                {
                    MedicalHistory = entity,
                    MedicalHistoryId = entity.Id,
                    SpecialistId = c.SpecialistId,
                    Specialist = consultationSpecialist,
                    Reason = c.Reason,
                    ConsultationDate = c.ConsultationDate,
                    Notes = c.Notes
                };
            }).ToList();

            return permission;
        }).ToList();
    }
}