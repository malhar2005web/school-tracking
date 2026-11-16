namespace SchoolTrack.Api.Middleware;

/// <summary>
/// Injects OWASP-recommended HTTP security headers to protect against clickjacking,
/// content sniffing, MIME confusion, and XSS vulnerabilities.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
            headers.TryAdd("X-XSS-Protection", "1; mode=block");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(self)");

            // Remove server identifying headers
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            if (context.Request.IsHttps)
            {
                headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSchoolTrackSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

