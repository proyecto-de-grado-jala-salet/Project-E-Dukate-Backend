using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Application.DTOs;
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
            return BadRequest(new { Errors = ex.Errors.ToList().Select(ex => ex.ErrorMessage) });
        }
        catch (Exception ex) when (ex.Message == "The chosen specialty does not exist")
        {
            return BadRequest(new { Error = ex.Message });
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
            return BadRequest(new { Errors = ex.Errors.ToList().Select(ex => ex.ErrorMessage) });
        }
        catch (Exception ex) when (ex.Message == "The chosen specialty does not exist")
        {
            return BadRequest(new { Error = ex.Message });
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
        var specialist = _service.GetSpecialistById(id);
        if (specialist == null) return NotFound();

        var response = new
        {
            specialist.Id,
            specialist.Names,
            specialist.LastNamePaternal,
            specialist.LastNameMaternal,
            specialist.MobileNumber,
            specialist.IdentityCard,
            specialist.PhoneNumber,
            specialist.Age,
            specialist.Gender,
            specialist.DateOfBirth,
            specialist.Address,
            specialist.Email,
            specialist.Password,
            specialty = specialist.Specialty?.TypeOfSpecialty,
            specialist.YearsOfExperience,
            specialist.SpecialistCode
        };

        return Ok(response);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var specialists = _service.GetAllSpecialists();

        var response = specialists.Select(specialist => new
        {
            specialist.Id,
            specialist.Names,
            specialist.LastNamePaternal,
            specialist.LastNameMaternal,
            specialist.MobileNumber,
            specialist.IdentityCard,
            specialist.PhoneNumber,
            specialist.Age,
            specialist.Gender,
            specialist.DateOfBirth,
            specialist.Address,
            specialist.Email,
            specialist.Password,
            specialty = specialist.Specialty?.TypeOfSpecialty,
            specialist.YearsOfExperience,
            specialist.SpecialistCode
        });

        return Ok(response);
    }
}