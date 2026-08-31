using Mdsweep.Domain.Passengers;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<PassengerAggregate>
{
    public void Configure(EntityTypeBuilder<PassengerAggregate> builder)
    {
        builder.ToTable("Passengers");
        builder.HasKey(passenger => passenger.Id);
        builder.Property(passenger => passenger.TenantId).HasColumnName("tenant_id");
        builder.Property(passenger => passenger.BrokerMemberId).HasMaxLength(100);
        builder.Property(passenger => passenger.FirstName).HasMaxLength(200);
        builder.Property(passenger => passenger.LastName).HasMaxLength(200);
        builder.HasIndex(passenger => new { passenger.TenantId, passenger.BrokerMemberId }).IsUnique();
    }
}
