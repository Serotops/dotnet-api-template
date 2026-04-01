using DotnetApiTemplate.API.Configuration;
using DotnetApiTemplate.API.Services;
using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Application.Interfaces.Services;
using DotnetApiTemplate.Application.Services;
using DotnetApiTemplate.Application.Validators;
using DotnetApiTemplate.Persistence;
using DotnetApiTemplate.Persistence.Interceptors;
using DotnetApiTemplate.Persistence.Repositories;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DotnetApiTemplate.API.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(
    this IServiceCollection services,
    IConfiguration config,
    IWebHostEnvironment environment
)
    {
        services.AddControllers(options =>
            {
                // Add our custom validation filter
                options.Filters.Add<DotnetApiTemplate.API.Filters.ValidationFilter>();
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // Disable automatic model state validation since we handle it in ValidationFilter
                options.SuppressModelStateInvalidFilter = true;
            });

        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ReportApiVersions = true;
            opt.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("x-api-version"),
                new MediaTypeApiVersionReader("x-api-version")
            );
        });

        services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services.AddSwaggerGen(option =>
        {
            option.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer",
                }
            );

            var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

            if (File.Exists(xmlPath))
            {
                option.IncludeXmlComments(xmlPath);
            }
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        // Only register PostgreSQL in non-Testing environments
        // Testing environment will register InMemory database in WebApplicationFactory
        if (!environment.IsEnvironment("Testing"))
        {
            services.AddDbContext<DotnetApiTemplateDbContext>((sp, options) =>
            {
                NpgsqlConnectionStringBuilder connectionString = new(
                    config.GetConnectionString("DefaultConnection")
                );

                connectionString.Username =
                    config.GetValue<string>("DB_USER") ?? connectionString.Username;
                connectionString.Password =
                    config.GetValue<string>("DB_PASSWORD") ?? connectionString.Password;

                var migrationsSchema = config.GetValue<string>("ConnectionStrings:Schema") ?? "public";

                options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());

                options.UseNpgsql(
                    connectionString.ConnectionString,
                    x => x.MigrationsHistoryTable("__EFMigrationsHistory", migrationsSchema)
                );
            });
        }

        services
        .AddControllers()
        .AddJsonOptions(x =>
            x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles
        );

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICarRepository, CarRepository>();

        services.AddScoped<ICarService, CarService>();

        // Register FluentValidation validators
        // Note: We DON'T use AddFluentValidationAutoValidation() here because we handle
        // validation manually in ValidationFilter to have full access to ErrorCodes
        services.AddValidatorsFromAssemblyContaining<CarUpsertDtoValidator>();

        services.AddHealthChecks();

        services.AddCors(c =>
        {
            c.AddPolicy(
                "AllowOrigin",
                options =>
                {
                    options
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders("Content-Disposition", "Content-Type");
                }
            );
        });

        services.AddRateLimiter();

        return services;
    }
}