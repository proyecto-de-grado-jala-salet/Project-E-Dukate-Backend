using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Entities;
using E_Dukate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : User
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _entities;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _entities = context.Set<T>();
    }

    public void Add(T entity)
    {
        _entities.Add(entity);
        _context.SaveChanges();
    }

    public void Update(T entity)
    {
        _entities.Update(entity);
        _context.SaveChanges();
    }

    public void Delete(Guid id)
    {
        var entity = _entities.Find(id);
        if (entity != null)
        {
            _entities.Remove(entity);
            _context.SaveChanges();
        }
    }

    public T? GetById(Guid id) => _entities.Find(id);
    public IEnumerable<T> GetAll() => _entities.ToList();
}