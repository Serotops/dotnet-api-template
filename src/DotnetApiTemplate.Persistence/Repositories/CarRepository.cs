using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiTemplate.Persistence.Repositories;

/// <summary>
/// Car repository implementation.
/// Inherits common CRUD operations from Repository and implements Car-specific methods.
/// </summary>
public class CarRepository(DotnetApiTemplateDbContext context)
    : Repository<Car>(context), ICarRepository
{
    // GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync are inherited from Repository<Car>
    // No need to implement them here!

    /// <summary>
    /// Retrieves cars with pagination and filtering.
    /// This is a Car-specific method that implements custom filtering logic.
    /// </summary>
    public async Task<PaginationResult<Car>> GetFilteredAsync(CarParams filterParams, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        // Filters
        if (!string.IsNullOrEmpty(filterParams.Make))
            query = query.Where(c => c.Make == filterParams.Make);

        if (!string.IsNullOrEmpty(filterParams.Model))
            query = query.Where(c => c.Model == filterParams.Model);

        if (filterParams.MinYear.HasValue)
            query = query.Where(c => c.Year >= filterParams.MinYear.Value);

        if (filterParams.MaxYear.HasValue)
            query = query.Where(c => c.Year <= filterParams.MaxYear.Value);

        if (filterParams.MinPrice.HasValue)
            query = query.Where(c => c.Price >= filterParams.MinPrice.Value);

        if (filterParams.MaxPrice.HasValue)
            query = query.Where(c => c.Price <= filterParams.MaxPrice.Value);

        if (filterParams.IsAvailable.HasValue)
            query = query.Where(c => c.IsAvailable == filterParams.IsAvailable.Value);

        // Sorting (always apply an OrderBy to avoid EF Core warning with Skip/Take)
        query = filterParams.Sort switch
        {
            "makeAsc" => query.OrderBy(c => c.Make),
            "makeDesc" => query.OrderByDescending(c => c.Make),
            "modelAsc" => query.OrderBy(c => c.Model),
            "modelDesc" => query.OrderByDescending(c => c.Model),
            "yearAsc" => query.OrderBy(c => c.Year),
            "yearDesc" => query.OrderByDescending(c => c.Year),
            "priceAsc" => query.OrderBy(c => c.Price),
            "priceDesc" => query.OrderByDescending(c => c.Price),
            _ => query.OrderByDescending(c => c.CreatedAt) // Default: newest first
        };

        // Total before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Pagination
        var skip = (filterParams.PageIndex - 1) * filterParams.PageSize;
        var data = await query.Skip(skip).Take(filterParams.PageSize).ToListAsync(cancellationToken);

        return new PaginationResult<Car>
        {
            PageIndex = filterParams.PageIndex,
            PageSize = filterParams.PageSize,
            TotalItems = totalItems,
            Data = data
        };
    }
}
