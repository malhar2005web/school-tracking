using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Api.Extensions;

/// <summary>
/// Configures liveness, readiness, and aggregate health check endpoints.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Registers API and PostgreSQL readiness health checks.
    /// </summary>
    public static IServiceCollection AddSchoolTrackHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("SchoolTrack API process is alive."), tags: ["live"]);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            builder.AddNpgSql(
                connectionString,
                name: "postgres",
                tags: ["ready"]);
        }

        builder.AddDbContextCheck<SchoolTrackDbContext>(
            name: "efcore-dbcontext",
            tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Maps `/health`, `/health/live`, and `/health/ready` JSON endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapSchoolTrackHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(ApiRoutes.Health, new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponseAsync
        });

        endpoints.MapHealthChecks(ApiRoutes.HealthLive, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = WriteHealthResponseAsync
        });

        endpoints.MapHealthChecks(ApiRoutes.HealthReady, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponseAsync
        });

        return endpoints;
    }

    private static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestampUtc = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}

