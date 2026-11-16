using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SchoolTrack.Api.Middleware;

/// <summary>
/// Centralized exception handler converting unhandled exceptions into structured RFC 7807 ProblemDetails responses.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of <see cref="GlobalExceptionHandler"/>.
    /// </summary>
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. TraceId={TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Handled client exception ({StatusCode}) for {Method} {Path}. TraceId={TraceId}",
                statusCode,
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId);
        }

        var detail = _environment.IsDevelopment() || statusCode < StatusCodes.Status500InternalServerError
            ? exception.Message
            : "An unexpected server error occurred. Please contact support with the traceId.";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestampUtc"] = DateTimeOffset.UtcNow;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request argument."),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized."),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Requested resource was not found."),
        OperationCanceledException => (499, "Request was cancelled by the client."),
        _ => (StatusCodes.Status500InternalServerError, "Internal server error.")
    };
}

