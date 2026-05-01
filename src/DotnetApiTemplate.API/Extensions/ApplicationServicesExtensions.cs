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
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

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
                options.Filters.Add<DotnetApiTemplate.API.Filters.ValidationFilter>();
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            })
            .AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles
            );

        services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("x-api-version"),
                new MediaTypeApiVersionReader("x-api-version")
            );
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

            option.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });

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

        var healthChecks = services.AddHealthChecks();

        if (!environment.IsEnvironment("Testing"))
        {
            NpgsqlConnectionStringBuilder connectionStringBuilder = new(
                config.GetConnectionString("DefaultConnection")
            );

            connectionStringBuilder.Username =
                config.GetValue<string>("DB_USER") ?? connectionStringBuilder.Username;
            connectionStringBuilder.Password =
                config.GetValue<string>("DB_PASSWORD") ?? connectionStringBuilder.Password;

            var connectionString = connectionStringBuilder.ConnectionString;
            var migrationsSchema = config.GetValue<string>("ConnectionStrings:Schema") ?? "public";

            services.AddDbContext<DotnetApiTemplateDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());

                options.UseNpgsql(
                    connectionString,
                    x => x.MigrationsHistoryTable("__EFMigrationsHistory", migrationsSchema)
                );
            });

            healthChecks.AddNpgSql(
                connectionString,
                name: "postgres",
                tags: new[] { "db", "ready" });
        }

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICarRepository, CarRepository>();

        services.AddScoped<ICarService, CarService>();

        services.AddValidatorsFromAssemblyContaining<CarUpsertDtoValidator>();

        // TODO: Restrict origins, headers, and methods for production environments
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

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        return services;
    }
}
