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

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken);
        if (entity is null) return;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
