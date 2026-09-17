using Mdsweep.Domain.Trips;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class JourneyConfiguration : IEntityTypeConfiguration<JourneyAggregate>
{
    public void Configure(EntityTypeBuilder<JourneyAggregate> builder)
    {
        builder.ToTable("journeys");

        builder.HasKey(journey => journey.Id);
        builder.Property(journey => journey.TenantId).HasMaxLength(14).IsRequired();
        builder.Property(journey => journey.GroupingType).IsRequired();
        builder.HasIndex(journey => journey.TenantId);
    }
}
