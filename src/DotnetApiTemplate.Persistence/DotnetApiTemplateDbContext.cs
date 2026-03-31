using DotnetApiTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiTemplate.Persistence;

public class DotnetApiTemplateDbContext(DbContextOptions<DotnetApiTemplateDbContext> options) : DbContext(options)
{
    public DbSet<Car> Cars => Set<Car>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DotnetApiTemplateDbContext).Assembly);
    }
}
