namespace TourShuttle.Application.Interfaces;

/// <summary>
/// Saves every change made through the repositories in one transaction.
/// Throws <see cref="Common.ConcurrencyConflictException"/> or <see cref="Common.UniqueConstraintException"/>
/// instead of database-specific exceptions.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
