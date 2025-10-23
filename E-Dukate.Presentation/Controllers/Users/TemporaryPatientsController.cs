using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Users;

namespace E_Dukate.Presentation.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
public class TemporaryPatientsController : ControllerBase
{
    private readonly TemporaryPatientService _temporaryPatientService;

    public TemporaryPatientsController(TemporaryPatientService temporaryPatientService)
    {
        _temporaryPatientService = temporaryPatientService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemporaryPatient([FromBody] CreateTemporaryPatientRequestDto request)
    {
        if (request == null || request.PatientData == null)
            return BadRequest(new { Error = "Invalid request data" });

        var result = await _temporaryPatientService.CreateTemporaryPatientAsync(request);
        
        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return Ok(new { 
            TemporaryPatientId = result.Value,
            Message = "Temporary patient created successfully" 
        });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmTemporaryPatient([FromBody] ConfirmTemporaryPatientRequestDto request)
    {
        var result = await _temporaryPatientService.ConfirmTemporaryPatientAsync(request.TemporaryPatientId);
        
        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return Ok(new { 
            PatientId = result.Value!.Id,
            Message = "Patient confirmed and created successfully",
            Patient = new {
                result.Value.Id,
                result.Value.Names,
                result.Value.LastNamePaternal,
                result.Value.LastNameMaternal,
                result.Value.IdentityCard,
                result.Value.MobileNumber
            }
        });
    }

    [HttpGet("whatsapp/{whatsAppNumber}")]
    public async Task<IActionResult> GetByWhatsAppNumber(string whatsAppNumber)
    {
        var result = await _temporaryPatientService.GetTemporaryPatientByWhatsAppAsync(whatsAppNumber);
        
        if (!result.IsSuccess)
            return NotFound(new { Error = result.ErrorMessage });

        var temporaryPatient = result.Value!;
        return Ok(new {
            temporaryPatient.Id,
            temporaryPatient.Names,
            temporaryPatient.LastNamePaternal,
            temporaryPatient.LastNameMaternal,
            temporaryPatient.IdentityCard,
            temporaryPatient.MobileNumber,
            temporaryPatient.Age,
            temporaryPatient.Gender,
            temporaryPatient.DateOfBirth,
            temporaryPatient.Address,
            temporaryPatient.IsConfirmed,
            temporaryPatient.ExpiresAt
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemporaryPatient(Guid id)
    {
        var result = await _temporaryPatientService.GetTemporaryPatientDtoAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { Error = result.ErrorMessage });

        return Ok(result.Value);
    }

    [HttpPost("clean-expired")]
    public async Task<IActionResult> CleanExpiredPatients()
    {
        var result = await _temporaryPatientService.CleanExpiredTemporaryPatientsAsync();
        
        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return Ok(new { Message = result.Value });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemporaryPatient(Guid id)
    {
        var result = await _temporaryPatientService.DeleteTemporaryPatientAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return Ok(new { Message = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTemporaryPatients()
    {
        var result = await _temporaryPatientService.GetAllTemporaryPatientsAsync();

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return Ok(new
        {
            TemporaryPatients = result.Value,
            Count = result.Value?.Count ?? 0
        });
    }

    [HttpGet("by-ci/{identityCard}")]
    public async Task<IActionResult> GetByIdentityCard(int identityCard)
    {
        var result = await _temporaryPatientService.GetTemporaryPatientByIdentityCardAsync(identityCard);

        if (!result.IsSuccess)
            return NotFound(new { Error = result.ErrorMessage });

        var temporaryPatient = result.Value!;
        return Ok(new
        {
            temporaryPatient.Id,
            temporaryPatient.Names,
            temporaryPatient.LastNamePaternal,
            temporaryPatient.LastNameMaternal,
            temporaryPatient.IdentityCard,
            temporaryPatient.MobileNumber,
            temporaryPatient.WhatsAppNumber,
            temporaryPatient.IsConfirmed
        });
    }
}