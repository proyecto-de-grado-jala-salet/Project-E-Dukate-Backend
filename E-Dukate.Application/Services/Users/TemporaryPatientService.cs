// Application/Services/Users/TemporaryPatientService.cs
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.MedicalHistories;

namespace E_Dukate.Application.Services.Users;

public class TemporaryPatientService
{
    private readonly IGenericRepository<TemporaryPatient> _temporaryRepository;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<MedicalHistory> _medicalHistoryRepository;
    private readonly IValidator<TemporaryPatientDto> _validator;

    public TemporaryPatientService(
        IGenericRepository<TemporaryPatient> temporaryRepository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<MedicalHistory> medicalHistoryRepository,
        IValidator<TemporaryPatientDto> validator)
    {
        _temporaryRepository = temporaryRepository;
        _patientRepository = patientRepository;
        _medicalHistoryRepository = medicalHistoryRepository;
        _validator = validator;
    }

    public async Task<ValueResult<Guid>> CreateTemporaryPatientAsync(CreateTemporaryPatientRequestDto request)
    {
        try
        {
            // Validar datos del paciente
            var validationResult = await _validator.ValidateAsync(request.PatientData);
            if (!validationResult.IsValid)
                return ValueResult<Guid>.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            // Verificar si ya existe un paciente temporal con el mismo CI y mismo WhatsApp
            var existingTemporary = await _temporaryRepository.GetAll()
                .FirstOrDefaultAsync(p =>
                    p.IdentityCard == request.PatientData.IdentityCard &&
                    p.WhatsAppNumber == request.WhatsAppNumber &&
                    !p.IsConfirmed &&
                    p.ExpiresAt > DateTime.UtcNow);

            if (existingTemporary != null)
            {
                // Actualizar el existente
                UpdateTemporaryPatient(existingTemporary, request.PatientData);
                existingTemporary.ExpiresAt = DateTime.UtcNow.AddHours(24);

                await _temporaryRepository.UpdateAsync(existingTemporary);
                return ValueResult<Guid>.Success(existingTemporary.Id);
            }

            // Verificar si ya existe un paciente REAL con el mismo CI
            var existingRealPatient = await _patientRepository.GetAll()
                .FirstOrDefaultAsync(p => p.IdentityCard == request.PatientData.IdentityCard);

            if (existingRealPatient != null)
            {
                // Si ya existe paciente real, crear temporal pero marcado como confirmado
                var temporaryPatient = new TemporaryPatient
                {
                    WhatsAppNumber = request.WhatsAppNumber,
                    Names = request.PatientData.Names,
                    LastNamePaternal = request.PatientData.LastNamePaternal,
                    LastNameMaternal = request.PatientData.LastNameMaternal,
                    MobileNumber = request.PatientData.MobileNumber,
                    IdentityCard = request.PatientData.IdentityCard,
                    PhoneNumber = request.PatientData.PhoneNumber,
                    Age = request.PatientData.Age,
                    Gender = request.PatientData.Gender,
                    DateOfBirth = request.PatientData.DateOfBirth,
                    Address = request.PatientData.Address,
                    IsConfirmed = true,
                    RealPatientId = existingRealPatient.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                await _temporaryRepository.AddAsync(temporaryPatient);
                return ValueResult<Guid>.Success(temporaryPatient.Id);
            }

            // Crear nuevo paciente temporal no confirmado
            var newTemporaryPatient = new TemporaryPatient
            {
                WhatsAppNumber = request.WhatsAppNumber,
                Names = request.PatientData.Names,
                LastNamePaternal = request.PatientData.LastNamePaternal,
                LastNameMaternal = request.PatientData.LastNameMaternal,
                MobileNumber = request.PatientData.MobileNumber,
                IdentityCard = request.PatientData.IdentityCard,
                PhoneNumber = request.PatientData.PhoneNumber,
                Age = request.PatientData.Age,
                Gender = request.PatientData.Gender,
                DateOfBirth = request.PatientData.DateOfBirth,
                Address = request.PatientData.Address,
                IsConfirmed = false,
                RealPatientId = null,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            await _temporaryRepository.AddAsync(newTemporaryPatient);
            return ValueResult<Guid>.Success(newTemporaryPatient.Id);
        }
        catch (Exception ex)
        {
            return ValueResult<Guid>.Failure($"Error creating temporary patient: {ex.Message}");
        }
    }

    public async Task<ValueResult<Patient>> ConfirmTemporaryPatientAsync(Guid temporaryPatientId)
    {
        try
        {
            // Buscar paciente temporal
            var temporaryPatient = await _temporaryRepository.GetByIdAsync(temporaryPatientId);
            if (temporaryPatient == null)
                return ValueResult<Patient>.Failure("Temporary patient not found");

            if (temporaryPatient.IsConfirmed && temporaryPatient.RealPatientId.HasValue)
            {
                // Ya está confirmado, retornar el paciente real
                var existingPatient = await _patientRepository.GetByIdAsync(temporaryPatient.RealPatientId.Value);
                if (existingPatient != null)
                    return ValueResult<Patient>.Success(existingPatient);
            }

            // Verificar si ya existe un paciente real con el mismo CI
            var existingRealPatient = await _patientRepository.GetAll()
                .FirstOrDefaultAsync(p => p.IdentityCard == temporaryPatient.IdentityCard);

            Patient realPatient;

            if (existingRealPatient != null)
            {
                // Usar el paciente existente
                realPatient = existingRealPatient;
            }
            else
            {
                // Crear nuevo paciente real
                realPatient = new Patient
                {
                    Names = temporaryPatient.Names,
                    LastNamePaternal = temporaryPatient.LastNamePaternal,
                    LastNameMaternal = temporaryPatient.LastNameMaternal,
                    MobileNumber = temporaryPatient.MobileNumber,
                    IdentityCard = temporaryPatient.IdentityCard,
                    PhoneNumber = temporaryPatient.PhoneNumber,
                    Age = temporaryPatient.Age,
                    Gender = temporaryPatient.Gender,
                    DateOfBirth = temporaryPatient.DateOfBirth,
                    Address = temporaryPatient.Address
                };

                // Crear historial médico para el nuevo paciente
                var medicalHistory = new MedicalHistory
                {
                    PatientId = realPatient.Id,
                    Patient = realPatient
                };

                realPatient.MedicalHistoryId = medicalHistory.Id;
                realPatient.MedicalHistory = medicalHistory;

                await _patientRepository.AddAsync(realPatient);
                await _medicalHistoryRepository.AddAsync(medicalHistory);
            }

            // Marcar temporal como confirmado
            temporaryPatient.IsConfirmed = true;
            temporaryPatient.RealPatientId = realPatient.Id;
            await _temporaryRepository.UpdateAsync(temporaryPatient);

            return ValueResult<Patient>.Success(realPatient);
        }
        catch (Exception ex)
        {
            return ValueResult<Patient>.Failure($"Error confirming temporary patient: {ex.Message}");
        }
    }

    public async Task<ValueResult<TemporaryPatient>> GetTemporaryPatientByWhatsAppAsync(string whatsAppNumber)
    {
        try
        {
            var temporaryPatient = await _temporaryRepository.GetAll()
                .Where(p => p.WhatsAppNumber == whatsAppNumber &&
                           !p.IsConfirmed &&
                           p.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (temporaryPatient == null)
                return ValueResult<TemporaryPatient>.Failure("No active temporary patient found");

            return ValueResult<TemporaryPatient>.Success(temporaryPatient);
        }
        catch (Exception ex)
        {
            return ValueResult<TemporaryPatient>.Failure($"Error getting temporary patient: {ex.Message}");
        }
    }

    public async Task<ValueResult<TemporaryPatientResponseDto>> GetTemporaryPatientDtoAsync(Guid temporaryPatientId)
    {
        try
        {
            var temporaryPatient = await _temporaryRepository.GetByIdAsync(temporaryPatientId);
            if (temporaryPatient == null)
                return ValueResult<TemporaryPatientResponseDto>.Failure("Temporary patient not found");

            var responseDto = new TemporaryPatientResponseDto
            {
                Id = temporaryPatient.Id,
                Names = temporaryPatient.Names,
                LastNamePaternal = temporaryPatient.LastNamePaternal,
                LastNameMaternal = temporaryPatient.LastNameMaternal,
                IdentityCard = temporaryPatient.IdentityCard,
                MobileNumber = temporaryPatient.MobileNumber,
                Age = temporaryPatient.Age,
                Gender = temporaryPatient.Gender,
                DateOfBirth = temporaryPatient.DateOfBirth,
                Address = temporaryPatient.Address,
                IsConfirmed = temporaryPatient.IsConfirmed,
                ExpiresAt = temporaryPatient.ExpiresAt
            };

            return ValueResult<TemporaryPatientResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            return ValueResult<TemporaryPatientResponseDto>.Failure($"Error getting temporary patient: {ex.Message}");
        }
    }

    public async Task<ValueResult<string>> CleanExpiredTemporaryPatientsAsync()
    {
        try
        {
            var expiredPatients = await _temporaryRepository.GetAll()
                .Where(p => p.ExpiresAt <= DateTime.UtcNow && !p.IsConfirmed)
                .ToListAsync();

            int deletedCount = 0;
            foreach (var patient in expiredPatients)
            {
                await _temporaryRepository.DeleteAsync(patient.Id);
                deletedCount++;
            }

            return ValueResult<string>.Success($"Cleaned {deletedCount} expired temporary patients");
        }
        catch (Exception ex)
        {
            return ValueResult<string>.Failure($"Error cleaning expired patients: {ex.Message}");
        }
    }

    public async Task<ValueResult<string>> DeleteTemporaryPatientAsync(Guid temporaryPatientId)
    {
        try
        {
            var temporaryPatient = await _temporaryRepository.GetByIdAsync(temporaryPatientId);
            if (temporaryPatient == null)
                return ValueResult<string>.Failure("Temporary patient not found");

            // Solo permitir eliminar pacientes temporales no confirmados
            if (temporaryPatient.IsConfirmed)
                return ValueResult<string>.Failure("Cannot delete a confirmed temporary patient");

            await _temporaryRepository.DeleteAsync(temporaryPatientId);
            return ValueResult<string>.Success("Temporary patient deleted successfully");
        }
        catch (Exception ex)
        {
            return ValueResult<string>.Failure($"Error deleting temporary patient: {ex.Message}");
        }
    }

    public async Task<ValueResult<List<TemporaryPatientResponseDto>>> GetAllTemporaryPatientsAsync()
    {
        try
        {
            var temporaryPatients = await _temporaryRepository.GetAll()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var responseDtos = temporaryPatients.Select(p => new TemporaryPatientResponseDto
            {
                Id = p.Id,
                Names = p.Names,
                LastNamePaternal = p.LastNamePaternal,
                LastNameMaternal = p.LastNameMaternal,
                IdentityCard = p.IdentityCard,
                MobileNumber = p.MobileNumber,
                Age = p.Age,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
                Address = p.Address,
                IsConfirmed = p.IsConfirmed,
                ExpiresAt = p.ExpiresAt
            }).ToList();

            return ValueResult<List<TemporaryPatientResponseDto>>.Success(responseDtos);
        }
        catch (Exception ex)
        {
            return ValueResult<List<TemporaryPatientResponseDto>>.Failure($"Error getting temporary patients: {ex.Message}");
        }
    }
    
    public async Task<ValueResult<TemporaryPatient>> GetTemporaryPatientByIdentityCardAsync(int identityCard)
    {
        try
        {
            var temporaryPatient = await _temporaryRepository.GetAll()
                .FirstOrDefaultAsync(p => p.IdentityCard == identityCard &&
                               !p.IsConfirmed &&
                               p.ExpiresAt > DateTime.UtcNow);

            if (temporaryPatient == null)
                return ValueResult<TemporaryPatient>.Failure("No active temporary patient found with this identity card");

            return ValueResult<TemporaryPatient>.Success(temporaryPatient);
        }
        catch (Exception ex)
        {
            return ValueResult<TemporaryPatient>.Failure($"Error getting temporary patient: {ex.Message}");
        }
    }

    private void UpdateTemporaryPatient(TemporaryPatient entity, TemporaryPatientDto dto)
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