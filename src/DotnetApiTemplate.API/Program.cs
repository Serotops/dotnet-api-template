using DotnetApiTemplate.API.Extensions;
using DotnetApiTemplate.API.Middlewares;
using DotnetApiTemplate.Persistence;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Exceptions;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails());

builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // HTTP Strict Transport Security in non-development environments.
    // TLS termination is usually handled by a reverse proxy/ingress; enable
    // UseHttpsRedirection here as well if the app terminates TLS itself.
    app.UseHsts();
}

app.UseCors("AllowOrigin");

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    var apiVersionDescriptionProvider =
        app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseStaticFiles();

    app.UseSwagger(option =>
    {
        option.RouteTemplate = "api/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
        {
            options.SwaggerEndpoint(
                $"/api/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
        options.RoutePrefix = "api";
        options.InjectStylesheet("/api/swagger-custom/swagger-custom-style.css");
    });

    // Open Swagger in the default browser on startup. launchSettings.json's
    // launchBrowser is only honored by IDEs and `dotnet watch`, so do it here
    // to also cover plain `dotnet run`. Opt out with OpenBrowserOnStartup=false.
    if (app.Configuration.GetValue("OpenBrowserOnStartup", true))
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                // Bound addresses are only populated once the server has started.
                var baseUrl = app.Urls.FirstOrDefault(u => u.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                    ?? app.Urls.FirstOrDefault()
                    ?? "https://localhost:7001";
                Process.Start(new ProcessStartInfo($"{baseUrl}/api") { UseShellExecute = true });
            }
            catch
            {
                // Best-effort only (e.g. headless/CI environments). Never fail startup.
            }
        });
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness: is the process up? No dependency checks, so a transient DB blip
// won't cause an orchestrator to kill an otherwise-healthy pod.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// Readiness: can we serve traffic? Includes dependency checks tagged "ready".
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Apply migrations on startup only when explicitly enabled (Database:MigrateOnStartup).
// In production prefer running migrations as a separate, controlled deploy step.
var migrateOnStartup = app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
if (migrateOnStartup && !app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.SetCommandTimeout(300);
    await db.Database.MigrateAsync();
}

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
