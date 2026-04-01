using DotnetApiTemplate.IntegrationTests.Fixtures;
using DotnetApiTemplate.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DotnetApiTemplate.IntegrationTests.Base;

/// <summary>
/// Base class for integration tests providing common setup and helper methods.
/// Implements IClassFixture to share the WebApplicationFactory across all tests in a class.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<ApiTestFactory>, IDisposable
{
    protected readonly ApiTestFactory Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly DotnetApiTemplateDbContext DbContext;

    protected IntegrationTestBase(ApiTestFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Scope = factory.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<DotnetApiTemplateDbContext>();
    }

    /// <summary>
    /// Sends a POST request with JSON content.
    /// </summary>
    protected async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T content)
    {
        var json = JsonSerializer.Serialize(content);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PostAsync(url, stringContent);
    }

    /// <summary>
    /// Sends a PUT request with JSON content.
    /// </summary>
    protected async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T content)
    {
        var json = JsonSerializer.Serialize(content);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PutAsync(url, stringContent);
    }

    /// <summary>
    /// Sends a PATCH request with JSON content.
    /// </summary>
    protected async Task<HttpResponseMessage> PatchAsJsonAsync<T>(string url, T content)
    {
        var json = JsonSerializer.Serialize(content);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PatchAsync(url, stringContent);
    }

    /// <summary>
    /// Deserializes the response content to the specified type.
    /// </summary>
    protected async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Clears the database between tests to ensure isolation.
    /// </summary>
    protected void ClearDatabase()
    {
        Factory.ClearDatabase();
    }

    /// <summary>
    /// Saves changes to the database and detaches all tracked entities.
    /// This ensures a clean state for the next database operation.
    /// </summary>
    protected async Task SaveAndClearTracking()
    {
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
    }

    public virtual void Dispose()
    {
        DbContext?.Dispose();
        Scope?.Dispose();
        GC.SuppressFinalize(this);
    }
}
