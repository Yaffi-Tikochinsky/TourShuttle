using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Infrastructure.Persistence.Configurations;

public class TourPickupPointConfiguration : IEntityTypeConfiguration<TourPickupPoint>
{
    public void Configure(EntityTypeBuilder<TourPickupPoint> builder)
    {
        // Composite primary key: a point appears at most once in each tour.
        builder.HasKey(tp => new { tp.TourId, tp.PickupPointId });

        builder.ToTable(t => t.HasCheckConstraint("CK_TourPickupPoints_Order", "\"Order\" >= 1"));

        // Two points of the same tour cannot share a position.
        builder.HasIndex(tp => new { tp.TourId, tp.Order }).IsUnique();

        builder.HasOne(tp => tp.Tour)
            .WithMany(t => t.PickupPoints)
            .HasForeignKey(tp => tp.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tp => tp.PickupPoint)
            .WithMany(p => p.Tours)
            .HasForeignKey(tp => tp.PickupPointId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
