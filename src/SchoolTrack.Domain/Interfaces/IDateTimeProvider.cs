namespace SchoolTrack.Domain.Interfaces;

/// <summary>
/// Abstraction for obtaining the current UTC time to keep domain logic testable.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

