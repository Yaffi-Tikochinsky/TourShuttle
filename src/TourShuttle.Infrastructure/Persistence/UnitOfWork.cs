using Microsoft.EntityFrameworkCore;
using Npgsql;
using TourShuttle.Application.Common;
using TourShuttle.Application.Interfaces;

namespace TourShuttle.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Saves all tracked changes in one transaction and translates EF Core / PostgreSQL exceptions
    /// into Application exceptions, so the layers above never depend on EF Core.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new UniqueConstraintException(ex);
        }
    }
}
