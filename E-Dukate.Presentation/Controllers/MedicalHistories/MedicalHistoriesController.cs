using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.DTOs.MedicalHistories;
using E_Dukate.Application.Services.MedicalHistories;
using System.Security.Claims;

namespace E_Dukate.Presentation.Controllers.MedicalHistories;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalHistoriesController : ControllerBase
{
    private readonly MedicalHistoryService _medicalHistoryService;

    public MedicalHistoriesController(MedicalHistoryService medicalHistoryService)
    {
        _medicalHistoryService = medicalHistoryService;
    }

    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Administrator,Specialist")]
    public async Task<IActionResult> GetMedicalHistoriesByPatientId(Guid patientId)
    {
        var medicalHistory = await _medicalHistoryService.GetByPatientIdAsync(patientId);
        if (medicalHistory == null)
            return NotFound("Medical History were not found for the specified patient.");

        return Ok(medicalHistory);
    }

    [HttpPost("permissions")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateEditingPermission([FromBody] PermissionRequestDto request)
    {
        var result = await _medicalHistoryService.UpdateEditingPermissionAsync(request);

        if (!result)
            return BadRequest("The permit could not be updated. Please verify that the medical history and specialist are present.");

        return Ok("Permit updated successfully");
    }

    [HttpDelete("permissions/{permissionId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeletePermission(Guid permissionId)
    {
        var result = await _medicalHistoryService.DeletePermissionAsync(permissionId);

        if (!result)
            return BadRequest("The permit could not be deleted. Please verify that the permit exists.");

        return Ok("Permit deleted successfully.");
    }

    [HttpPut("histories/{medicalHistoryId}/specialists/{specialistId}/status")]
    [Authorize(Roles = "Administrator,Specialist")]
    public async Task<IActionResult> UpdateMedicalHistoryStatus(
        [FromRoute] Guid medicalHistoryId,
        [FromRoute] Guid specialistId,
        [FromBody] UpdateMedicalHistoryStatusDto request)
    {
        if (User.IsInRole("Specialist"))
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            if (userId != specialistId)
                return Unauthorized("You are not allowed to update another specialist's status.");
        }

        var result = await _medicalHistoryService.UpdateMedicalHistoryStatusAsync(
            medicalHistoryId,
            specialistId,
            request);

        if (!result)
            return BadRequest("The status could not be updated. Please verify that the medical history and specialist exist and have permissions.");

        return Ok("Medical history status updated successfully.");
    }
}