namespace SchoolTrack.Domain.Common;

/// <summary>
/// Base entity with sequential UUID v7 primary key and UTC audit timestamps.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

