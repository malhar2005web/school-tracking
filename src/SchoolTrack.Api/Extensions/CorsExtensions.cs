namespace SchoolTrack.Api.Extensions;

/// <summary>
/// Configures production CORS policies supporting Blazor Web, MAUI Hybrid, and SignalR cross-origin streaming.
/// </summary>
public static class CorsExtensions
{
    public const string DefaultCorsPolicy = "SchoolTrackCors";

    public static IServiceCollection AddSchoolTrackCors(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[]
            {
                "http://localhost:5000",
                "https://localhost:5001",
                "http://localhost:5080",
                "https://localhost:5443",
                "http://localhost:5081",
                "http://localhost:5200",
                "https://localhost:7150",
                "https://localhost:7189",
                "app://localhost",
                "http://localhost"
            };

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicy, builder =>
            {
                builder
                    .WithOrigins(configuredOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}

