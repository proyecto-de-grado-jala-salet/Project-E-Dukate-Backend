using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.DTOs.MedicalHistories;
using E_Dukate.Application.Services.MedicalHistories;

namespace E_Dukate.Presentation.Controllers.MedicalHistories;

[ApiController]
[Route("api/[controller]")]
public class MedicalConsultationsController : ControllerBase
{
    private readonly MedicalConsultationService _medicalConsultationService;

    public MedicalConsultationsController(MedicalConsultationService medicalConsultationService)
    {
        _medicalConsultationService = medicalConsultationService;
    }

    [HttpPost("histories/{medicalHistoryId}/specialists/{specialistId}/permissions/{permissionId}/consultation")]
    public async Task<IActionResult> CreateMedicalConsultation(
        [FromRoute] Guid medicalHistoryId,
        [FromRoute] Guid specialistId,
        [FromRoute] Guid permissionId,
        [FromBody] UpdateMedicalConsultationDto request)
    {
        var result = await _medicalConsultationService.CreateMedicalConsultationAsync(
            medicalHistoryId,
            specialistId,
            permissionId,
            request);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok("Consulta médica creada correctamente");
    }

    [HttpPut("{consultationId}")]
    public async Task<IActionResult> UpdateMedicalConsultation(
        [FromRoute] Guid consultationId,
        [FromBody] UpdateMedicalConsultationDto request)
    {
        var result = await _medicalConsultationService.UpdateMedicalConsultationAsync(consultationId, request);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok("Consulta médica actualizada correctamente");
    }
}