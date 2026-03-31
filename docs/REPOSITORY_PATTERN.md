# Generic Repository Pattern

This template implements the **Generic Repository Pattern** to eliminate code duplication across repositories and provide consistent data access patterns.

## Table of Contents
- [Architecture](#architecture)
- [Benefits](#benefits)
- [Structure](#structure)
- [Creating New Repositories](#creating-new-repositories)
- [Examples](#examples)
- [Advanced Usage](#advanced-usage)

---

## Architecture

```
IRepository<T>                          (Generic interface - Application layer)
    ↓
Repository<T>                           (Generic implementation - Persistence layer)
    ↓
ICarRepository : IRepository<Car>       (Entity-specific interface - Application layer)
    ↓
CarRepository : Repository<Car>         (Entity-specific implementation - Persistence layer)
```

**Key Principle:**
- Common CRUD operations are implemented **once** in `Repository<T>`
- Entity-specific repositories **only implement** their unique logic

---

## Benefits

✅ **Eliminates 80% of repository code duplication**
- GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync are inherited
- Only implement entity-specific logic (filtering, complex queries, etc.)

✅ **Consistent behavior across all repositories**
- All entities get the same reliable CRUD operations
- Automatic Id generation and timestamp management

✅ **Easier maintenance**
- Fix bugs in one place (`Repository<T>`)
- Changes propagate to all repositories automatically

✅ **Faster development**
- New repositories require minimal code
- Focus on business logic, not boilerplate

✅ **Type-safe**
- Generic constraints ensure only entities can be used
- Compile-time safety for repository operations

✅ **Flexible**
- Override base methods when needed (all methods are `virtual`)
- Full access to DbContext and DbSet through protected members

---

## Structure

### 1. IRepository<T> - Generic Interface

**Location:** `DotnetApiTemplate.Application/Interfaces/Repositories/IRepository.cs`

Defines common CRUD operations for all entities:

```csharp
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}
```

### 2. Repository<T> - Generic Implementation

**Location:** `DotnetApiTemplate.Persistence/Repositories/Repository.cs`

Implements common CRUD operations with:
- Automatic Id generation (if Guid.Empty)
- Automatic CreatedAt timestamp on Add
- Automatic ModifiedAt timestamp on Update
- All methods are `virtual` for override flexibility

```csharp
public class Repository<T>(DotnetApiTemplateDbContext context) : IRepository<T>
    where T : AuditableEntity
{
    protected readonly DotnetApiTemplateDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    // ... other CRUD methods
}
```

**Protected Members Available:**
- `_context` - Full DbContext access for complex queries
- `_dbSet` - The DbSet<T> for the entity type

### 3. Entity-Specific Interface

**Example:** `DotnetApiTemplate.Application/Interfaces/Repositories/ICarRepository.cs`

Inherits from `IRepository<T>` and adds entity-specific methods:

```csharp
public interface ICarRepository : IRepository<Car>
{
    // Common CRUD inherited from IRepository<Car>

    // Only Car-specific methods defined here
    Task<PaginationResult<Car>> GetFilteredAsync(CarParams filterParams);
}
```

### 4. Entity-Specific Repository

**Example:** `DotnetApiTemplate.Persistence/Repositories/CarRepository.cs`

Inherits from `Repository<T>` and implements entity-specific interface:

```csharp
public class CarRepository(DotnetApiTemplateDbContext context)
    : Repository<Car>(context), ICarRepository
{
    // GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync are FREE!

    // Only implement Car-specific methods
    public async Task<PaginationResult<Car>> GetFilteredAsync(CarParams filterParams)
    {
        var query = _dbSet.AsQueryable(); // Use inherited _dbSet

        // Car-specific filtering logic...
    }
}
```

---

## Creating New Repositories

### Step 1: Create Entity-Specific Interface (Application Layer)

```csharp
// DotnetApiTemplate.Application/Interfaces/Repositories/IProductRepository.cs
using DotnetApiTemplate.Domain.Entities;

namespace DotnetApiTemplate.Application.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    // Common CRUD inherited automatically

    // Add only Product-specific methods
    Task<IEnumerable<Product>> GetByCategoryAsync(string category);
    Task<IEnumerable<Product>> GetInStockAsync();
}
```

### Step 2: Create Entity-Specific Repository (Persistence Layer)

```csharp
// DotnetApiTemplate.Persistence/Repositories/ProductRepository.cs
using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiTemplate.Persistence.Repositories;

public class ProductRepository(DotnetApiTemplateDbContext context)
    : Repository<Product>(context), IProductRepository
{
    // GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync are inherited!

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetInStockAsync()
    {
        return await _dbSet
            .Where(p => p.Stock > 0)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
```

### Step 3: Register in DI Container (Program.cs)

```csharp
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

**That's it!** You've created a fully functional repository with CRUD operations in just a few lines of code.

---

## Examples

### Example 1: Simple Repository (No Custom Methods)

If your entity only needs basic CRUD:

```csharp
// Interface
public interface ICategoryRepository : IRepository<Category>
{
    // That's it! All CRUD methods inherited
}

// Implementation
public class CategoryRepository(DotnetApiTemplateDbContext context)
    : Repository<Category>(context), ICategoryRepository
{
    // No code needed! All CRUD operations inherited
}
```

### Example 2: Repository with Custom Queries

```csharp
public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Order>> GetPendingOrdersAsync();
    Task<decimal> GetTotalRevenueAsync();
}

public class OrderRepository(DotnetApiTemplateDbContext context)
    : Repository<Order>(context), IOrderRepository
{
    public async Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetPendingOrdersAsync()
    {
        return await _dbSet
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalRevenueAsync()
    {
        return await _dbSet
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount);
    }
}
```

### Example 3: Overriding Base Methods

If you need custom behavior for a base method:

```csharp
public class UserRepository(DotnetApiTemplateDbContext context)
    : Repository<User>(context), IUserRepository
{
    // Override GetAllAsync to exclude deleted users
    public override async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbSet
            .Where(u => !u.IsDeleted)
            .ToListAsync();
    }

    // Override GetByIdAsync to include related data
    public override async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(u => u.Profile)
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    // Custom methods
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }
}
```

---

## Advanced Usage

### Accessing DbContext

The base `Repository<T>` exposes `_context` for complex operations:

```csharp
public async Task<int> BulkUpdatePricesAsync(decimal percentage)
{
    return await _context.Database
        .ExecuteSqlRawAsync(
            "UPDATE Products SET Price = Price * {0}",
            1 + (percentage / 100));
}
```

### Using Transactions

```csharp
public async Task TransferStockAsync(Guid fromWarehouse, Guid toWarehouse, int quantity)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        var from = await _dbSet.FindAsync(fromWarehouse);
        var to = await _dbSet.FindAsync(toWarehouse);

        if (from == null || to == null)
            throw new InvalidOperationException("Warehouse not found");

        from.Stock -= quantity;
        to.Stock += quantity;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Soft Delete Pattern

Override DeleteAsync to implement soft delete:

```csharp
public class ProductRepository(DotnetApiTemplateDbContext context)
    : Repository<Product>(context), IProductRepository
{
    public override async Task DeleteAsync(Guid id)
    {
        var product = await _dbSet.FindAsync(id);
        if (product == null) return;

        // Soft delete instead of hard delete
        product.IsDeleted = true;
        product.ModifiedAt = DateTime.UtcNow;

        _dbSet.Update(product);
        await _context.SaveChangesAsync();
    }

    // Override GetAllAsync to exclude deleted items
    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .ToListAsync();
    }
}
```

---

## Code Comparison: Before vs After

### Before (Traditional Repository)

```csharp
public class ProductRepository : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<Product> AddAsync(Product product)
    {
        if (product.Id == Guid.Empty)
            product.Id = Guid.NewGuid();

        product.CreatedAt = DateTime.UtcNow;

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        product.ModifiedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }

    // Only this is actually product-specific!
    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        return await _context.Products
            .Where(p => p.Category == category)
            .ToListAsync();
    }
}
```

**Lines of Code:** ~50 lines

### After (Generic Repository Pattern)

```csharp
public class ProductRepository(DotnetApiTemplateDbContext context)
    : Repository<Product>(context), IProductRepository
{
    // GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync are inherited!

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Where(p => p.Category == category)
            .ToListAsync();
    }
}
```

**Lines of Code:** ~12 lines

**Reduction:** **76% less code!**

---

## Best Practices

1. **Keep entity-specific logic in repositories** - Don't put business logic here, that belongs in services

2. **Use `_dbSet` for queries** - It's already configured and ready to use

3. **Override base methods when needed** - All methods are `virtual` for flexibility

4. **Include related data in overrides** - If an entity commonly needs related data, override GetByIdAsync to include it

5. **Use meaningful method names** - GetByCategoryAsync is clearer than FilterAsync

6. **Document complex queries** - Add XML comments for methods with complex logic

7. **Don't expose IQueryable** - Return concrete types (IEnumerable, List) to prevent query modification outside the repository

---

## Resources

- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Generic Repository Pattern](https://www.c-sharpcorner.com/article/generic-repository-pattern-in-asp-net-core/)
- [EF Core Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)
