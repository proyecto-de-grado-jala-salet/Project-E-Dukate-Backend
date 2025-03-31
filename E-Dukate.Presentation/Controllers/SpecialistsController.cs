using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Application.DTOs; // Agregado para SpecialistDto
using E_Dukate.Domain.Entities;
using FluentValidation;

namespace E_Dukate.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecialistsController : ControllerBase
{
    private readonly SpecialistService _service;

    public SpecialistsController(SpecialistService service)
    {
        _service = service;
    }

    [HttpPost]
    public IActionResult Add([FromBody] SpecialistDto dto)
    {
        try
        {
            _service.Register(dto);
            return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] SpecialistDto dto)
    {
        try
        {
            _service.Update(id, dto);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex) when (ex.Message == "Specialist not found.")
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
        var specialist = _service.FindById(id);
        if (specialist == null) return NotFound();
        return Ok(specialist);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var specialists = _service.ListAll();
        return Ok(specialists);
    }
}