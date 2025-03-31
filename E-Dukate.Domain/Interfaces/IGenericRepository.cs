namespace E_Dukate.Domain.Interfaces;

public interface IGenericRepository<T> where T : Entities.User
{
    void Add(T entity);
    void Update(T entity);
    void Delete(Guid id);
    T? GetById(Guid id);
    IEnumerable<T> GetAll();
}