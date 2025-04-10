using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Application.DTOs.Common;

namespace E_Dukate.Application.Services;

public abstract class BaseService<T, TDto> where T : Entity where TDto : class
{
    protected readonly IGenericRepository<T> Repository;
    protected readonly IValidator<TDto> Validator;

    protected BaseService(IGenericRepository<T> repository, IValidator<TDto> validator)
    {
        Repository = repository;
        Validator = validator;
    }

    public virtual Result Register(TDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var entity = MapToEntity(dto);
        Repository.Add(entity);
        return Result.Success();
    }

    public virtual Result Update(Guid id, TDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var existing = Repository.GetById(id);
        if (existing == null)
            return Result.Failure($"{typeof(T).Name} not found.");

        UpdateEntity(existing, dto);
        Repository.Update(existing);
        return Result.Success();
    }

    public void Delete(Guid id)
    {
        var entity = Repository.GetById(id);
        if (entity == null) throw new Exception($"{typeof(T).Name} not found.");
        Repository.Delete(id);
    }

    public T? FindById(Guid id) => Repository.GetById(id);

    public IEnumerable<T> ListAll() => Repository.GetAll();

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(PaginationParams pagination)
    {
        var query = Repository.GetAll();
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    protected abstract T MapToEntity(TDto dto);
    protected abstract void UpdateEntity(T entity, TDto dto);
}