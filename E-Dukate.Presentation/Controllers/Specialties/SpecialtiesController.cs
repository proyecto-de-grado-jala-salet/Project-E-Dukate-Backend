using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Specialties;
using E_Dukate.Application.DTOs.Specialties;
using FluentValidation;
using E_Dukate.Domain.Entities.Specialties;

namespace E_Dukate.Presentation.Controllers.Specialties;

public class SpecialtiesController : BaseController<Specialty, SpecialtyDto>
{
    public SpecialtiesController(SpecialtyService service) : base(service) { }

    [HttpPost]
    public override IActionResult Add([FromBody] SpecialtyDto dto)
    {
        try
        {
            Service.Register(dto);
            return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}