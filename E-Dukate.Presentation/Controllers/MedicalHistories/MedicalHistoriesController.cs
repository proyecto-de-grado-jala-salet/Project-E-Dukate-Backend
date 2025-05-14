using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.MedicalHistories;

namespace E_Dukate.Presentation.Controllers.MedicalHistories;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class MedicalHistoriesController : ControllerBase
{
    private readonly MedicalHistoryService _medicalHistoryService;

    public MedicalHistoriesController(MedicalHistoryService medicalHistoryService)
    {
        _medicalHistoryService = medicalHistoryService;
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetByPatientId(Guid patientId)
    {
        var result = await _medicalHistoryService.GetByPatientIdAsync(patientId);
        if (!result.IsSuccess)
            return NotFound(new { Error = result.ErrorMessage });

        return Ok(result.Value);
    }
}