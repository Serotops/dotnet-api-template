using DotnetApiTemplate.API.Extensions;
using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Application.Interfaces.Services;
using DotnetApiTemplate.Application.MappingProfiles;
using DotnetApiTemplate.Application.Services;
using DotnetApiTemplate.Application.Validators;
using DotnetApiTemplate.Middlewares;
using DotnetApiTemplate.Persistence;
using DotnetApiTemplate.Persistence.Repositories;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

string region = "";
string secretName = "";
Exception? secretManagerException = null;
// Add AWS Secret Manager Conficuration, except on Development environments
if (!builder.Environment.IsDevelopment())
{


    //var awsConfig = builder.Configuration.GetSection("AWS");

    //region = awsConfig.GetValue<string>("Region");
    //secretName = awsConfig.GetValue<string>("SecretName");



    //try
    //{
    //    builder.Configuration.AddAmazonSecretsManager(region, secretName);
    //}
    //catch (Exception e)
    //{
    //    secretManagerException = e;


    //}

}

//Add Serilog middleware
builder.Host.UseSerilog(
    (context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext()
);

// Add services to the container.
//Call static method that's adding all services we need instead of adding them one by one in here.
builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// CORS
app.UseCors("AllowOrigin");

// Rate Limiter (must be before authentication/authorization so rejections happen early)
app.UseRateLimiter();

// Custom middlewares (order matters!)
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

var apiVersionDescriptionProvider =
    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
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
            description.GroupName.ToUpperInvariant()
        );
    }
    options.RoutePrefix = "api";

    options.InjectStylesheet("/api/swagger-custom/swagger-custom-style.css");
    //options.InjectJavascript("/swagger-custom/swagger-custom-script.js", "text/javascript"); To use later
}); 

app.UseMiddleware<ResponseWrapperMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

// Configure Logger
var logger = services.GetRequiredService<ILogger<Program>>();

if (secretManagerException != null)
{
    logger.LogInformation("Error on secret manager");
    logger.LogInformation($"Region : {region}");
    logger.LogInformation($"Secret Name : {secretName}");
    throw secretManagerException;
}



logger.LogInformation("Starting API Template...");
logger.LogInformation($"... on {app.Environment.EnvironmentName} environment");

// Skip database migrations in testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        using var context = services.GetRequiredService<DotnetApiTemplateDbContext>();
        context.Database.SetCommandTimeout(300);
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occured during migration or data seeding");
        throw;
    }
}

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
