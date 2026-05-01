using DotnetApiTemplate.API.Extensions;
using DotnetApiTemplate.Middlewares;
using DotnetApiTemplate.Persistence;
using Asp.Versioning.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails());

builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseCors("AllowOrigin");
app.UseRateLimiter();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

var apiVersionDescriptionProvider =
    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
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
}

app.UseMiddleware<ResponseWrapperMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// WARNING: Auto-migration at startup is convenient for development but can cause
// race conditions in multi-instance production deployments. Consider using a
// dedicated migration step in your CI/CD pipeline for production environments.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DotnetApiTemplateDbContext>();
    db.Database.SetCommandTimeout(300);
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program { }
