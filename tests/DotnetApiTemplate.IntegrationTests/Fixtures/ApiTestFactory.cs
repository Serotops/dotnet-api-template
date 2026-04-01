using DotnetApiTemplate.Persistence;
using DotnetApiTemplate.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotnetApiTemplate.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Replaces the PostgreSQL database with an in-memory database for testing.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDatabase_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DotnetApiTemplateDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove any DbContextOptions
            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions));
            if (optionsDescriptor != null)
            {
                services.Remove(optionsDescriptor);
            }

            // Add in-memory database for testing with unique name per factory instance
            services.AddDbContext<DotnetApiTemplateDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });
        });
    }

    /// <summary>
    /// Creates a new service scope for accessing scoped services.
    /// The caller is responsible for disposing the scope.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return Services.CreateScope();
    }

    /// <summary>
    /// Clears all data from the database.
    /// Useful for resetting state between tests.
    /// </summary>
    public void ClearDatabase()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DotnetApiTemplateDbContext>();
        context.Cars.RemoveRange(context.Cars);
        context.SaveChanges();
    }
}
