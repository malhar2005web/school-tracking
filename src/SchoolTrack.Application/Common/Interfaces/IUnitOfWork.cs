namespace SchoolTrack.Application.Common.Interfaces;

/// <summary>
/// Coordinates atomic persistence of changes across application operations.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all pending changes to the backing data store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

