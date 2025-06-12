using E_Dukate.Application.DTOs.Common;
using E_Dukate.Application.DTOs.FAQ;
using E_Dukate.Domain.Entities.FAQ;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Application.Services.FAQ;

public class FaqService : BaseService<Faq, FaqDto>
{
    private readonly IGenericRepository<Faq> _repository;

    public FaqService(
        IGenericRepository<Faq> repository,
        IValidator<FaqDto> validator)
        : base(repository, validator)
    {
        _repository = repository;
    }

    public override Result Register(FaqDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var existingFaq = _repository.GetAll()
            .FirstOrDefault(f => f.Question.ToLower() == dto.Question.ToLower());

        if (existingFaq != null)
            return Result.Failure($"The question '{dto.Question}' already exists.");

        var faq = MapToEntity(dto);
        _repository.Add(faq);
        return Result.Success();
    }

    public override Result Update(Guid id, FaqDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var existing = _repository.GetById(id);
        if (existing == null)
            return Result.Failure("FAQ not found.");

        var duplicateFaq = _repository.GetAll()
            .FirstOrDefault(f => f.Question.ToLower() == dto.Question.ToLower() && f.Id != id);

        if (duplicateFaq != null)
            return Result.Failure($"The question '{dto.Question}' already exists.");

        UpdateEntity(existing, dto);
        _repository.Update(existing);
        return Result.Success();
    }

    protected override Faq MapToEntity(FaqDto dto)
    {
        return new Faq
        {
            Question = dto.Question,
            Answer = dto.Answer
        };
    }

    protected override void UpdateEntity(Faq entity, FaqDto dto)
    {
        entity.Question = dto.Question;
        entity.Answer = dto.Answer;
    }

    public async Task<(IEnumerable<Faq> Items, int TotalCount)> SearchFaqsAsync(string searchTerm, PaginationParams pagination)
    {
        var query = _repository.GetAll();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(f =>
                f.Question.ToLower().Contains(searchTerm) ||
                f.Answer.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(f => f.Question)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}