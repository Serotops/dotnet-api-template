using DotnetApiTemplate.Domain.Entities;

namespace DotnetApiTemplate.Application.Interfaces.Repositories;

/// <summary>
/// Generic repository interface providing common CRUD operations for all entities.
/// All entity-specific repositories should inherit from this interface.
/// </summary>
/// <typeparam name="T">The entity type, must inherit from AuditableEntity</typeparam>
public interface IRepository<T> where T : AuditableEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
