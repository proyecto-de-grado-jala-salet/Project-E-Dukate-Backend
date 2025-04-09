using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Domain.Primitives;
using FluentValidation;

namespace E_Dukate.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController<T, TDto> : ControllerBase
    where T : Entity
    where TDto : class
{
    protected readonly BaseService<T, TDto> Service;

    protected BaseController(BaseService<T, TDto> service)
    {
        Service = service;
    }

    [HttpPost]
    public virtual IActionResult Add([FromBody] TDto dto)
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
    }

    [HttpPut("{id}")]
    public virtual IActionResult Update(Guid id, [FromBody] TDto dto)
    {
        try
        {
            Service.Update(id, dto);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        Service.Delete(id);
        return NoContent();
    }

    [HttpGet("{id}")]
    public virtual IActionResult GetById(Guid id)
    {
        var entity = Service.FindById(id);
        if (entity == null) return NotFound();
        return Ok(entity);
    }

    [HttpGet]
    public virtual IActionResult GetAll()
    {
        var entities = Service.ListAll();
        return Ok(entities);
    }
}