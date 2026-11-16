namespace SchoolTrack.Shared.Models;

/// <summary>
/// Standard response envelope for SchoolTrack REST endpoints.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public string? TraceId { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Success", string? traceId = null) =>
        new()
        {
            Success = true,
            Message = message,
            Data = data,
            TraceId = traceId
        };

    public static ApiResponse<T> Fail(string message, string? traceId = null) =>
        new()
        {
            Success = false,
            Message = message,
            TraceId = traceId
        };
}

