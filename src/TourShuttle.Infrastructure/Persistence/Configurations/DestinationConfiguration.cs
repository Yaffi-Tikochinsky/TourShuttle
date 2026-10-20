using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Infrastructure.Persistence.Configurations;

public class DestinationConfiguration : IEntityTypeConfiguration<Destination>
{
    public void Configure(EntityTypeBuilder<Destination> builder)
    {
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.City).HasMaxLength(100).IsRequired();

        // Two destinations with one name would be indistinguishable in every list. The service checks first
        // for a clear message; the index is the guarantee when two requests arrive at the same moment.
        builder.HasIndex(d => d.Name).IsUnique();
    }
}
