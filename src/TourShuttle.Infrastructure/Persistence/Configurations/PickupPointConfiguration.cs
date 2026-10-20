using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Infrastructure.Persistence.Configurations;

public class PickupPointConfiguration : IEntityTypeConfiguration<PickupPoint>
{
    public void Configure(EntityTypeBuilder<PickupPoint> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(200).IsRequired();
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();

        // Tours are filtered by the cities of their stops, and the city list is read for the search box.
        builder.HasIndex(p => p.City);
    }
}
