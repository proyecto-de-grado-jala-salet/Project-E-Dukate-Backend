using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Specialties;
using E_Dukate.Application.DTOs.Specialties;
using FluentValidation;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Application.DTOs.Common;

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

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string searchTerm, [FromQuery] PaginationParams pagination)
    {
        var (items, totalCount) = await _service.SearchSpecialtiesAsync(searchTerm, pagination);
        if (!items.Any())
        {
            return Ok(new { Message = "No se encontraron resultados de lo buscado" });
        }

        var response = items.Select(specialty => new
        {
            specialty.Id,
            specialty.TypeOfSpecialty
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