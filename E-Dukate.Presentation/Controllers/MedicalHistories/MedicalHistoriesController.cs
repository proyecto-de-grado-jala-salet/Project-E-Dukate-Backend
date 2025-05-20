using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.DTOs.MedicalHistories;
using E_Dukate.Application.Services.MedicalHistories;

namespace E_Dukate.Presentation.Controllers.MedicalHistories;

[ApiController]
[Route("api/[controller]")]
public class MedicalHistoriesController : ControllerBase
{
    private readonly MedicalHistoryService _medicalHistoryService;

    public MedicalHistoriesController(MedicalHistoryService medicalHistoryService)
    {
        _medicalHistoryService = medicalHistoryService;
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetMedicalHistoriesByPatientId(Guid patientId)
    {
        var medicalHistory = await _medicalHistoryService.GetByPatientIdAsync(patientId);
        if (medicalHistory == null)
            return NotFound("No se encontró el historial médico para el paciente especificado.");

        return Ok(medicalHistory);
    }

    [HttpPost("permissions")]
    public async Task<IActionResult> UpdateEditingPermission([FromBody] PermissionRequestDto request)
    {
        var result = await _medicalHistoryService.UpdateEditingPermissionAsync(request);

        if (!result)
            return BadRequest("No se pudo actualizar el permiso. Verifique que el historial médico y el especialista existan.");

        return Ok("Permiso actualizado correctamente");
    }

    [HttpPut("histories/{medicalHistoryId}/specialists/{specialistId}/status")]
    public async Task<IActionResult> UpdateMedicalHistoryStatus(
        [FromRoute] Guid medicalHistoryId,
        [FromRoute] Guid specialistId,
        [FromBody] UpdateMedicalHistoryStatusDto request)
    {
        var result = await _medicalHistoryService.UpdateMedicalHistoryStatusAsync(
            medicalHistoryId,
            specialistId,
            request);

        if (!result)
            return BadRequest("No se pudo actualizar el estado. Verifique que el historial médico y el especialista existan y tengan permisos.");

        return Ok("Estado del historial médico actualizado correctamente");
    }
}