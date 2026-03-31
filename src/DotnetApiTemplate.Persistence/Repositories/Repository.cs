using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiTemplate.Persistence.Repositories;

/// <summary>
/// Generic repository implementation providing common CRUD operations for all entities.
/// Entity-specific repositories should inherit from this class and implement their specific interface.
/// </summary>
/// <typeparam name="T">The entity type, must inherit from AuditableEntity</typeparam>
public class Repository<T>(DotnetApiTemplateDbContext context) : IRepository<T>
    where T : AuditableEntity
{
    protected readonly DotnetApiTemplateDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Retrieves all entities.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary>
    /// Adds a new entity to the repository.
    /// Automatically generates Id and sets CreatedAt timestamp.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Updates an existing entity.
    /// Automatically sets ModifiedAt timestamp.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes an entity by its unique identifier.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity is null) return;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
