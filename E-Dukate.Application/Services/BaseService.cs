using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using FluentValidation;

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

    public virtual void Register(TDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var entity = MapToEntity(dto);
        Repository.Add(entity);
    }

    public virtual void Update(Guid id, TDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var existing = Repository.GetById(id);
        if (existing == null)
            throw new Exception($"{typeof(T).Name} not found.");

        UpdateEntity(existing, dto);
        Repository.Update(existing);
    }

    public void Delete(Guid id)
    {
        var entity = Repository.GetById(id);
        if (entity == null) throw new Exception($"{typeof(T).Name} not found.");
        Repository.Delete(id);
    }

    public T? FindById(Guid id) => Repository.GetById(id);
    public IEnumerable<T> ListAll() => Repository.GetAll();

    protected abstract T MapToEntity(TDto dto);
    protected abstract void UpdateEntity(T entity, TDto dto);
}