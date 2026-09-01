using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskManager.Auth;
using TaskManager.Configuration;
using TaskManager.Data;
using TaskManager.Errors;
using TaskManager.Services;

namespace TaskManager.Composition;

public static class RegisterServices
{
    public static IServiceCollection AddTaskManagerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddMemoryCache();
        services.AddScoped<TaskStatsService>();
        services.AddScoped<TopicSearchService>();
        services.AddScoped<RagAccessService>();
        services.AddHttpClient<RagClientService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<RagOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient<RagEmbedClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<RagOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<AuthService>();
        services.AddScoped<TaskService>();
        services.AddScoped<TopicService>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<JwtTokenService>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddJwtAuthentication();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Hardliq", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the JWT only (no Bearer prefix)."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddCors(configuration);

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

        return services;
    }

    private sealed class JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions) : IPostConfigureOptions<JwtBearerOptions>
    {
        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            if (name is not null && name != JwtBearerDefaults.AuthenticationScheme)
            {
                return;
            }

            var jwt = jwtOptions.Value;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
            };
        }
    }

    private static IServiceCollection AddCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOrigins = configuration["Cors:AllowedOrigins"]
            ?? throw new InvalidOperationException("Cors:AllowedOrigins is not configured.");

        var allowedOrigins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        return services;
    }
}
