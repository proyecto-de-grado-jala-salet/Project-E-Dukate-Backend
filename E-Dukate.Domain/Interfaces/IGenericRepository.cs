namespace E_Dukate.Domain.Interfaces;

public interface IGenericRepository<T> where T : Primitives.Entity
{
    void Add(T entity);
    void Update(T entity);
    void Delete(Guid id);
    T? GetById(Guid id);
    IQueryable<T> GetAll();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<T?> GetByIdAsync(Guid id);
}