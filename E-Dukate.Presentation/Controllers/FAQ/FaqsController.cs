using E_Dukate.Application.DTOs.Common;
using E_Dukate.Application.DTOs.FAQ;
using E_Dukate.Application.Services.FAQ;
using E_Dukate.Domain.Entities.FAQ;
using Microsoft.AspNetCore.Mvc;

namespace E_Dukate.Presentation.Controllers.FAQ;

[ApiController]
[Route("api/[controller]")]
public class FaqsController : BaseController<Faq, FaqDto>
{
    private readonly FaqService _faqService;

    public FaqsController(FaqService faqService) : base(faqService)
    {
        _faqService = faqService;
    }

    [HttpPost]
    public override IActionResult Add([FromBody] FaqDto dto)
    {
        var result = _faqService.Register(dto);
        if (!result.IsSuccess)
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });

        return CreatedAtAction(nameof(GetById), new { id = Guid.NewGuid() }, dto);
    }

    [HttpPut("{id}")]
    public override IActionResult Update(Guid id, [FromBody] FaqDto dto)
    {
        var result = _faqService.Update(id, dto);
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage.Contains("not found"))
                return NotFound(new { Error = result.ErrorMessage });
            return BadRequest(new { Errors = result.ErrorMessage.Split(", ").ToList() });
        }

        return NoContent();
    }

    [HttpGet("{id}")]
    public override IActionResult GetById(Guid id)
    {
        var faq = _faqService.FindById(id);
        if (faq == null)
            return NotFound();

        return Ok(new
        {
            faq.Id,
            faq.Question,
            faq.Answer
        });
    }

    [HttpGet]
    public override async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var (items, totalCount) = await _faqService.GetPagedAsync(pagination);
        var response = items.Select(faq => new
        {
            faq.Id,
            faq.Question,
            faq.Answer
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

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string searchTerm, [FromQuery] PaginationParams pagination)
    {
        var (items, totalCount) = await _faqService.SearchFaqsAsync(searchTerm, pagination);
        if (!items.Any())
        {
            return Ok(new { Message = "No se encontraron resultados de lo buscado" });
        }

        var response = items.Select(faq => new
        {
            faq.Id,
            faq.Question,
            faq.Answer
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