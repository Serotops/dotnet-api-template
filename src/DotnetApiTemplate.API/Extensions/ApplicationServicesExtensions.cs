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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

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
        .AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ReportApiVersions = true;
            opt.ApiVersionReader = ApiVersionReader.Combine(
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
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
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

        var jwtSection = config.GetSection("Jwt");
        var signingKey = jwtSection.GetValue<string>("SigningKey");

        // The signing key is a secret: supply it via environment variable
        // (Jwt__SigningKey), user-secrets, or a secret manager -- never commit it.
        // Fail fast rather than start with a missing/weak key (HS256 needs >= 256 bits).
        // Skipped under the Testing environment, where authentication is stubbed.
        if (!environment.IsEnvironment("Testing")
            && (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing or too short. Provide at least 32 bytes via the " +
                "Jwt__SigningKey environment variable, user-secrets, or a secret manager.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection.GetValue<string>("Issuer"),
                    ValidAudience = jwtSection.GetValue<string>("Audience"),
                    IssuerSigningKey = string.IsNullOrEmpty(signingKey)
                        ? null
                        : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
                };
            });

        services.AddAuthorization();

        // The demo token endpoint mints credential-free JWTs. Refuse to start if someone
        // enables it outside Development, so a misconfigured deploy fails loudly instead
        // of silently exposing an authentication bypass.
        if (config.GetValue("Auth:EnableDemoTokenEndpoint", false) && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Auth:EnableDemoTokenEndpoint is true outside the Development environment. " +
                "The demo token endpoint must never be enabled in production — remove the flag " +
                "and implement a real credential-verifying authentication flow.");
        }

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddDbContext<AppDbContext>((sp, options) =>
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

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICarRepository, CarRepository>();
        services.AddScoped<ICarService, CarService>();

        services.AddValidatorsFromAssemblyContaining<CarUpsertDtoValidator>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

        var rateLimitSection = config.GetSection("RateLimiting");
        var permitLimit = rateLimitSection.GetValue("PermitLimit", 100);
        var windowSeconds = rateLimitSection.GetValue("WindowSeconds", 10);
        var queueLimit = rateLimitSection.GetValue("QueueLimit", 0);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global fixed-window limiter partitioned by client IP. Adjust or replace
            // with per-endpoint policies as your API grows.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimit
                    });
            });
        });

        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(c =>
        {
            c.AddPolicy(
                "AllowOrigin",
                options =>
                {
                    if (allowedOrigins.Length == 0)
                    {
                        options.AllowAnyOrigin();
                    }
                    else
                    {
                        options
                            .WithOrigins(allowedOrigins)
                            .AllowCredentials();
                    }

                    options
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders("Content-Disposition", "Content-Type");
                }
            );
        });

        return services;
    }
}
