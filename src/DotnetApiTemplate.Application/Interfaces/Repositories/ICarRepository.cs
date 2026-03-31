using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Domain.Entities;

namespace DotnetApiTemplate.Application.Interfaces.Repositories;

/// <summary>
/// Car repository interface.
/// Inherits common CRUD operations from IRepository and adds Car-specific methods.
/// </summary>
public interface ICarRepository : IRepository<Car>
{
    // Common CRUD methods (GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync)
    // are inherited from IRepository<Car>

    // Only Car-specific methods are defined here
    Task<PaginationResult<Car>> GetFilteredAsync(CarParams filterParams);
}
