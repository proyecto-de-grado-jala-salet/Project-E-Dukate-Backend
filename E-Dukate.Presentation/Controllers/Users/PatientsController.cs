using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace E_Dukate.Presentation.Controllers.Users;

public class PatientsController : BaseController<Patient, PatientDto>
{
    private readonly PatientService _patientService;

    public PatientsController(PatientService service) : base(service)
    {
        _patientService = service;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string searchTerm, [FromQuery] PaginationParams pagination)
    {
        var (items, totalCount) = await _patientService.SearchPatientsAsync(searchTerm, pagination);
        if (!items.Any())
        {
            return Ok(new { Message = "No se encontraron resultados de lo buscado" });
        }

        var response = items.Select(patient => new
        {
            patient.Id,
            patient.Names,
            patient.LastNamePaternal,
            patient.LastNameMaternal,
            patient.MobileNumber,
            patient.IdentityCard,
            patient.PhoneNumber,
            patient.Age,
            patient.Gender,
            patient.DateOfBirth,
            patient.Address
        });

        return Ok(new
        {
            Items = response,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        });
    }
}