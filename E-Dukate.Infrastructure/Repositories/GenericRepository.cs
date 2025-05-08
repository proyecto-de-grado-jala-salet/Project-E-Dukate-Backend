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

    public async Task AddAsync(T entity)
    {
        await _entities.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        var existingEntity = await _entities.FindAsync(entity.Id);
        if (existingEntity == null)
        {
            throw new Exception($"{typeof(T).Name} with ID {entity.Id} not found.");
        }

        _context.Entry(existingEntity).CurrentValues.SetValues(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _entities.FindAsync(id);
        if (entity != null)
        {
            _entities.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<T?> GetByIdAsync(Guid id) => await _entities.FindAsync(id);

    public IQueryable<T> GetAll() => _entities.AsQueryable();
    
    public void Add(T entity) => AddAsync(entity).GetAwaiter().GetResult();
    public void Update(T entity) => UpdateAsync(entity).GetAwaiter().GetResult();
    public void Delete(Guid id) => DeleteAsync(id).GetAwaiter().GetResult();
    public T? GetById(Guid id) => GetByIdAsync(id).GetAwaiter().GetResult();
}