using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;
using FluentValidation;

namespace E_Dukate.Presentation.Controllers.Users;

public class SpecialistsController : BaseController<Specialist, SpecialistDto>
{
    private readonly SpecialistService _specialistService;

    public SpecialistsController(SpecialistService service) : base(service)
    {
        _specialistService = service;
    }

    [HttpPost]
    public override IActionResult Add([FromBody] SpecialistDto dto)
    {
        try
        {
            _specialistService.Register(dto);
            return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (Exception ex) when (ex.Message == "The chosen specialty does not exist")
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public override IActionResult Update(Guid id, [FromBody] SpecialistDto dto)
    {
        try
        {
            _specialistService.Update(id, dto);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
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

    [HttpGet("{id}")]
    public override IActionResult GetById(Guid id)
    {
        var specialist = _specialistService.GetSpecialistById(id);
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
    public override IActionResult GetAll()
    {
        var specialists = _specialistService.GetAllSpecialists();
        var response = specialists.Select(specialists => new
        {
            specialists.Id,
            specialists.Names,
            specialists.LastNamePaternal,
            specialists.LastNameMaternal,
            specialists.MobileNumber,
            specialists.IdentityCard,
            specialists.PhoneNumber,
            specialists.Age,
            specialists.Gender,
            specialists.DateOfBirth,
            specialists.Address,
            specialists.Email,
            specialists.Password,
            specialty = specialists.Specialty?.TypeOfSpecialty,
            specialists.YearsOfExperience,
            specialists.SpecialistCode
        });
        return Ok(response);
    }
}