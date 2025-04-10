using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Specialties;
using E_Dukate.Application.DTOs.Specialties;
using FluentValidation;
using E_Dukate.Domain.Entities.Specialties;

namespace E_Dukate.Presentation.Controllers.Specialties;

public class SpecialtiesController : BaseController<Specialty, SpecialtyDto>
{
    private readonly SpecialtyService _service;

    public SpecialtiesController(SpecialtyService service) : base(service)
    {
        _service = service;
    }

    [HttpPost]
    public override IActionResult Add([FromBody] SpecialtyDto dto)
    {
        var result = _service.Register(dto);
        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
    }

    [HttpPut("{id}")]
    public override IActionResult Update(Guid id, [FromBody] SpecialtyDto dto)
    {
        var result = _service.Update(id, dto);
        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return NoContent();
    }
}