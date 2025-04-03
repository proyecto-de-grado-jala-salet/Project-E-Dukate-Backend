using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Application.DTOs; // Agregado para PatientDto
using E_Dukate.Domain.Entities;
using FluentValidation;
using System.Linq;

namespace E_Dukate.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly PatientService _service;

    public PatientsController(PatientService service)
    {
        _service = service;
    }

    [HttpPost]
    public IActionResult Add([FromBody] PatientDto dto)
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
    public IActionResult Update(Guid id, [FromBody] PatientDto dto)
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
        catch (Exception ex) when (ex.Message == "Patient not found.")
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
        var patient = _service.FindById(id);
        if (patient == null) return NotFound();
        return Ok(patient);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var patients = _service.ListAll();
        return Ok(patients);
    }
}