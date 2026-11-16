using SchoolTrack.Domain.Interfaces;

namespace SchoolTrack.Infrastructure.Services;

/// <summary>
/// Production system clock implementation returning UTC timestamps.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

