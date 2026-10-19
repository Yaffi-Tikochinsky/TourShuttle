using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Infrastructure.Persistence.Configurations;

public class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.ToTable(t =>
        {
            // Safety net in the database itself: even a bug in the code cannot store these.
            t.HasCheckConstraint("CK_Tours_Capacity", "\"Capacity\" > 0");
            t.HasCheckConstraint("CK_Tours_AvailableSeats", "\"AvailableSeats\" >= 0 AND \"AvailableSeats\" <= \"Capacity\"");
            t.HasCheckConstraint("CK_Tours_MaxSeatsPerBooking", "\"MaxSeatsPerBooking\" >= 1 AND \"MaxSeatsPerBooking\" <= \"Capacity\"");
            t.HasCheckConstraint("CK_Tours_Price", "\"Price\" >= 0");
            t.HasCheckConstraint("CK_Tours_ReturnAfterDeparture", "\"ReturnAt\" > \"DepartureAt\"");
        });

        builder.Property(t => t.Price).HasPrecision(10, 2);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        // EF adds the original Version to the WHERE clause of every UPDATE and DELETE.
        builder.Property(t => t.Version).IsConcurrencyToken();

        builder.HasOne(t => t.Destination)
            .WithMany(d => d.Tours)
            .HasForeignKey(t => t.DestinationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Driver)
            .WithMany(u => u.DrivenTours)
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        // Tours are filtered and sorted by departure time.
        builder.HasIndex(t => t.DepartureAt);
    }
}
