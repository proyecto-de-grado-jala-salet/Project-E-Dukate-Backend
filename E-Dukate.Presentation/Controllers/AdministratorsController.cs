using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Application.DTOs; // Agregado para AdministratorDto
using E_Dukate.Domain.Entities;
using FluentValidation;

namespace E_Dukate.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministratorsController : ControllerBase
{
    private readonly AdministratorService _service;

    public AdministratorsController(AdministratorService service)
    {
        _service = service;
    }

    [HttpPost]
    public IActionResult Add([FromBody] AdministratorDto dto)
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
    public IActionResult Update(Guid id, [FromBody] AdministratorDto dto)
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
        catch (Exception ex) when (ex.Message == "Administrator not found.")
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
        var administrator = _service.FindById(id);
        if (administrator == null) return NotFound();
        return Ok(administrator);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var administrators = _service.ListAll();
        return Ok(administrators);
    }
}