using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SchoolTrack.Api.Extensions;

/// <summary>
/// Configures partition-based and policy-based rate limiting to prevent DDoS and brute-force attacks.
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicy = "AuthPolicy";
    public const string HardwarePolicy = "HardwarePolicy";
    public const string GeneralPolicy = "GeneralPolicy";

    public static IServiceCollection AddSchoolTrackRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                var problemDetails = new
                {
                    type = "https://httpstatuses.com/429",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit threshold exceeded. Please retry after cooldown period.",
                    instance = context.HttpContext.Request.Path.Value
                };
                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, token);
            };

            // 1. Auth policy: Strict fixed window for login & token refresh (15 req/min per IP)
            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 15,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // 2. Hardware telemetry policy: High throughput sliding window for GPS & RFID (150 req/min per device)
            options.AddPolicy(HardwarePolicy, httpContext =>
            {
                var deviceCode = httpContext.Request.Headers["X-Device-Code"].ToString();
                if (string.IsNullOrWhiteSpace(deviceCode))
                {
                    deviceCode = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                }

                return RateLimitPartition.GetSlidingWindowLimiter(deviceCode, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 150,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
            });

            // 3. General API policy: Token bucket for standard queries (60 req/min with burst up to 20)
            options.AddPolicy(GeneralPolicy, httpContext =>
            {
                var partitionKey = httpContext.User.Identity?.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 80,
                    TokensPerPeriod = 60,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
            });
        });

        return services;
    }
}

