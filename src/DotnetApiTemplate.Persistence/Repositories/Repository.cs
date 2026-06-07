using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiTemplate.Persistence.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T>
    where T : Entity
{
    protected AppDbContext Context { get; } = context;
    protected DbSet<T> DbSet { get; } = context.Set<T>();

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    /// <summary>
    /// Retrieves all entities.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new entity to the repository.
    /// Automatically generates Id and sets CreatedAt timestamp.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        await DbSet.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>
    /// Updates an existing entity.
    /// Automatically sets ModifiedAt timestamp.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes an entity by its unique identifier.
    /// Virtual to allow derived repositories to override if needed.
    /// </summary>
    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await DbSet.FindAsync([id], cancellationToken);
        if (entity is null) return;

        DbSet.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
