namespace TourShuttle.Application.Interfaces;

/// <summary>
/// Saves every change made through the repositories in one transaction.
/// Throws <see cref="Common.ConcurrencyConflictException"/>, <see cref="Common.UniqueConstraintException"/>
/// or <see cref="Common.ForeignKeyConstraintException"/> instead of database-specific exceptions.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Runs work that needs more than one save as a single transaction: either every save
    /// is committed, or none is.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken ct);
}
