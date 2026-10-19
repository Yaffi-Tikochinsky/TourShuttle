using Microsoft.EntityFrameworkCore;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<PickupPoint> PickupPoints => Set<PickupPoint>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourPickupPoint> TourPickupPoints => Set<TourPickupPoint>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Loads every IEntityTypeConfiguration<T> in this assembly (the Configurations folder).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RenewTourVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RenewTourVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Gives every modified tour a new Version. EF Core still sends the original Version in the
    /// UPDATE's WHERE clause, so an update based on stale data matches no row and EF throws
    /// DbUpdateConcurrencyException. Doing it here means no service has to remember it.
    /// </summary>
    private void RenewTourVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Tour>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }
}
