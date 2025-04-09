using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Application.DTOs;
using FluentValidation;

namespace E_Dukate.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecialtiesController : ControllerBase
{
    private readonly SpecialtyService _service;

    public SpecialtiesController(SpecialtyService service)
    {
        _service = service;
    }

    [HttpPost]
    public IActionResult Add([FromBody] SpecialtyDto dto)
    {
        try
        {
            _service.Register(dto);
            return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors.ToList().Select(ex => ex.ErrorMessage) });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] SpecialtyDto dto)
    {
        try
        {
            _service.Update(id, dto);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors.ToList().Select(ex => ex.ErrorMessage) });
        }
        catch (Exception ex) when (ex.Message == "Specialty not found.")
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        _service.Delete(id);
        return NoContent();
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var specialty = _service.FindById(id);
        if (specialty == null) return NotFound();
        return Ok(specialty);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var specialties = _service.ListAll();
        return Ok(specialties);
    }
}