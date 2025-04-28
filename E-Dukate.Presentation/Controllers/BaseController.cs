using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services;
using E_Dukate.Domain.Primitives;
using FluentValidation;
using E_Dukate.Application.DTOs.Common;

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
        var result = Service.Register(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
    }

    [HttpPut("{id}")]
    public virtual IActionResult Update(Guid id, [FromBody] TDto dto)
    {
        var result = Service.Update(id, dto);
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage.Contains("not found"))
                return NotFound(new { Error = result.ErrorMessage });
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return NoContent();
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
    public virtual async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var (items, totalCount) = await Service.GetPagedAsync(pagination);
        return Ok(new
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        });
    }
}