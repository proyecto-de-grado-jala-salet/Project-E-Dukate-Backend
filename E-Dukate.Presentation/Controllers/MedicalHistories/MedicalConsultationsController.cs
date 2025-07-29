using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.DTOs.MedicalHistories;
using E_Dukate.Application.Services.MedicalHistories;
using System.Security.Claims;
using E_Dukate.Application.DTOs.Common;
using Microsoft.AspNetCore.Http;
using System.IO;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Infrastructure.Data;

namespace E_Dukate.Presentation.Controllers.MedicalHistories;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalConsultationsController : ControllerBase
{
    private readonly MedicalConsultationService _medicalConsultationService;
    private readonly IGenericRepository<MedicalDocument> _documentRepository;
    private readonly IGenericRepository<MedicalHistoryPermission> _permissionRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly AppDbContext _context;

    public MedicalConsultationsController(
        MedicalConsultationService medicalConsultationService,
        IGenericRepository<MedicalDocument> documentRepository,
        IGenericRepository<MedicalHistoryPermission> permissionRepository,
        IWebHostEnvironment environment,
        AppDbContext context)
    {
        _medicalConsultationService = medicalConsultationService;
        _documentRepository = documentRepository;
        _permissionRepository = permissionRepository;
        _environment = environment;
        _context = context;
    }

    [HttpPost("histories/{medicalHistoryId}/specialists/{specialistId}/permissions/{permissionId}/consultation")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> CreateMedicalConsultation(
        [FromRoute] Guid medicalHistoryId,
        [FromRoute] Guid specialistId,
        [FromRoute] Guid permissionId,
        [FromBody] UpdateMedicalConsultationDto request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
        if (userId != specialistId)
            return Unauthorized("You do not have permission to create a consultation for another specialist.");

        var result = await _medicalConsultationService.CreateMedicalConsultationAsync(
            medicalHistoryId,
            specialistId,
            permissionId,
            request);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok("Medical consultation created correctly.");
    }

    [HttpPut("{consultationId}")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> UpdateMedicalConsultation(
        [FromRoute] Guid consultationId,
        [FromBody] UpdateMedicalConsultationDto request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
        var result = await _medicalConsultationService.CanSpecialistEditConsultationAsync(consultationId, userId);
        if (!result.IsSuccess)
            return Unauthorized("You are not authorized to edit this consultation.");

        var updateResult = await _medicalConsultationService.UpdateMedicalConsultationAsync(consultationId, request);

        if (!updateResult.IsSuccess)
            return BadRequest(updateResult.ErrorMessage);

        return Ok("Medical consultation updated correctly.");
    }

    [HttpDelete("{consultationId}")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> DeleteMedicalConsultation([FromRoute] Guid consultationId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
        var result = await _medicalConsultationService.DeleteMedicalConsultationAsync(consultationId, userId);
        if (!result.IsSuccess)
            return Unauthorized("You are not authorized to delete this consultation.");

        return Ok("Medical consultation deleted successfully.");
    }

    [HttpGet("histories/{medicalHistoryId}/specialists/{specialistId}/permissions/{permissionId}/consultations")]
    [Authorize(Roles = "Administrator,Specialist")]
    public async Task<IActionResult> GetSpecialistConsultations(
        [FromRoute] Guid medicalHistoryId,
        [FromRoute] Guid specialistId,
        [FromRoute] Guid permissionId,
        [FromQuery] PaginationParams pagination)
    {
        var result = await _medicalConsultationService.GetSpecialistConsultationsAsync(
            medicalHistoryId,
            specialistId,
            permissionId,
            pagination);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok(result.Value);
    }

    [HttpPost("permissions/{permissionId}/documents")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> UploadDocument(Guid permissionId, IFormFile file)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var permission = await _context.MedicalHistoryPermissions
                .FirstOrDefaultAsync(p => p.Id == permissionId && p.SpecialistId == Guid.Parse(userId!) && p.CanEdit);

            if (permission == null)
            {
                return Forbid("No tienes permisos para subir documentos.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No se proporcionó un archivo válido.");
            }

            var medicalHistory = await _context.MedicalHistories
                .FirstOrDefaultAsync(h => h.Permissions.Any(p => p.Id == permissionId));

            if (medicalHistory == null)
            {
                return NotFound("Historial médico no encontrado.");
            }

            var patientId = medicalHistory.PatientId;
            var specialistId = permission.SpecialistId;
            var documentId = Guid.NewGuid();
            var fileName = file.FileName;
            var filePath = Path.Combine("MedicalFiles", patientId.ToString(), specialistId.ToString(), $"{documentId}.pdf");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var document = new MedicalDocument
            {
                PermissionId = permissionId,
                FileName = fileName,
                FilePath = filePath,
                UploadDate = DateTime.UtcNow
            };

            _context.MedicalDocuments.Add(document);
            await _context.SaveChangesAsync();

            return Ok(new { DocumentId = documentId.ToString() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error al subir el documento: {ex.Message}" });
        }
    }

    [HttpGet("documents/{documentId}")]
    [Authorize(Roles = "Specialist,Administrator")]
    public async Task<IActionResult> GetDocument(Guid documentId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var document = await _context.MedicalDocuments
                .Include(d => d.Permission)
                .ThenInclude(p => p!.MedicalHistory)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                return NotFound("Documento no encontrado.");
            }

            if (User.IsInRole("Specialist"))
            {
                var medicalHistoryId = document.Permission!.MedicalHistoryId;
                var hasPermission = await _context.MedicalHistoryPermissions
                    .AnyAsync(p => p.MedicalHistoryId == medicalHistoryId && p.SpecialistId == Guid.Parse(userId!));

                if (!hasPermission)
                {
                    return Forbid("No tienes permisos para ver este documento.");
                }
            }

            var filePath = document.FilePath;
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Archivo no encontrado en el servidor.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/pdf", document.FileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error al obtener el documento: {ex.Message}" });
        }
    }

    [HttpDelete("documents/{documentId}")]
    [Authorize(Roles = "Specialist,Administrator")]
    public async Task<IActionResult> DeleteDocument(Guid documentId)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                return NotFound("Document not found.");
            }

            if (User.IsInRole("Specialist"))
            {
                var permission = await _permissionRepository.GetAll()
                    .FirstOrDefaultAsync(p => p.Id == document.PermissionId);

                if (permission == null || permission.SpecialistId != userId)
                {
                    return Unauthorized("You do not have permission to delete this document.");
                }

                if (!permission.CanEdit)
                {
                    return Unauthorized("You do not have editing permissions.");
                }
            }

            if (System.IO.File.Exists(document.FilePath))
            {
                System.IO.File.Delete(document.FilePath);
                Console.WriteLine($"DeleteDocument - File deleted: {document.FilePath}");
            }
            else
            {
                Console.WriteLine($"DeleteDocument - File not found: {document.FilePath}");
            }

            await _documentRepository.DeleteAsync(documentId);
            await _context.SaveChangesAsync();

            return Ok("Document deleted successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error al eliminar el documento: {ex.Message}" });
        }
    }
}