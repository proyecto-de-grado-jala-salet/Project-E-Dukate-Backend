using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using E_Dukate.Infrastructure.Data;

namespace E_Dukate.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : Entity
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

    public IQueryable<T> GetAll() => _entities.AsQueryable(); // Cambiado a IQueryable
}