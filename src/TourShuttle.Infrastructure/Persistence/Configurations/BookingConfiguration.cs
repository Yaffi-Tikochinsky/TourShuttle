using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Bookings_SeatsCount", "\"SeatsCount\" > 0");
            t.HasCheckConstraint("CK_Bookings_TotalPrice", "\"TotalPrice\" >= 0");
        });

        builder.Property(b => b.TotalPrice).HasPrecision(10, 2);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(b => b.Tour)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite foreign key: the (tour, pickup point) pair must exist in TourPickupPoints,
        // so a booking can never use a point that is not on its tour's route, and a point
        // cannot be removed from a tour while bookings still use it.
        builder.HasOne(b => b.TourPickupPoint)
            .WithMany(tp => tp.Bookings)
            .HasForeignKey(b => new { b.TourId, b.PickupPointId })
            .OnDelete(DeleteBehavior.Restrict);

        // One active booking per customer per tour. Cancelled bookings are kept as history,
        // so the filter limits the rule to active rows (a PostgreSQL partial index).
        builder.HasIndex(b => new { b.TourId, b.UserId })
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
    }
}
