using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.DTOs.MedicalHistories;
using E_Dukate.Application.Services.MedicalHistories;
using System.Security.Claims;

namespace E_Dukate.Presentation.Controllers.MedicalHistories;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalConsultationsController : ControllerBase
{
    private readonly MedicalConsultationService _medicalConsultationService;

    public MedicalConsultationsController(MedicalConsultationService medicalConsultationService)
    {
        _medicalConsultationService = medicalConsultationService;
    }

    [HttpPost("histories/{medicalHistoryId}/specialists/{specialistId}/permissions/{permissionId}/consultation")]
    [Authorize(Roles = "Administrator,Specialist")]
    public async Task<IActionResult> CreateMedicalConsultation(
        [FromRoute] Guid medicalHistoryId,
        [FromRoute] Guid specialistId,
        [FromRoute] Guid permissionId,
        [FromBody] UpdateMedicalConsultationDto request)
    {
        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            if (userId != specialistId)
                return Unauthorized("You do not have permission to create consultation for another specialist.");
        }

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
    [Authorize(Roles = "Administrator,Specialist")]
    public async Task<IActionResult> UpdateMedicalConsultation(
        [FromRoute] Guid consultationId,
        [FromBody] UpdateMedicalConsultationDto request)
    {
        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var result = await _medicalConsultationService.CanSpecialistEditConsultationAsync(consultationId, userId);
            if (!result.IsSuccess)
                return Unauthorized("You are not authorized to edit this consultation.");
        }

        var updateResult = await _medicalConsultationService.UpdateMedicalConsultationAsync(consultationId, request);

        if (!updateResult.IsSuccess)
            return BadRequest(updateResult.ErrorMessage);

        return Ok("Medical consultation updated correctly.");
    }

    [HttpDelete("{consultationId}")]
    [Authorize(Roles = "Administrator,Specialist")]
    public async Task<IActionResult> DeleteMedicalConsultation([FromRoute] Guid consultationId)
    {
        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var result = await _medicalConsultationService.DeleteMedicalConsultationAsync(consultationId, userId);
            if (!result.IsSuccess)
                return Unauthorized("You are not authorized to delete this consultation.");
        }
        else
        {
            var result = await _medicalConsultationService.DeleteMedicalConsultationAsync(consultationId);
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);
        }

        return Ok("Medical consultation deleted successfully.");
    }
}