using DotnetApiTemplate.Domain.Entities;

namespace DotnetApiTemplate.Application.Interfaces.Repositories;

/// <summary>
/// Generic repository interface providing common CRUD operations for all entities.
/// All entity-specific repositories should inherit from this interface.
/// </summary>
/// <typeparam name="T">The entity type, must inherit from Entity</typeparam>
public interface IRepository<T> where T : Entity
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity</param>
    /// <returns>The entity if found; otherwise null</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities.
    /// </summary>
    /// <returns>A collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository.
    /// Automatically generates Id and sets CreatedAt timestamp if entity is AuditableEntity.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <returns>The added entity with generated values</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// Automatically sets ModifiedAt timestamp if entity is AuditableEntity.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
