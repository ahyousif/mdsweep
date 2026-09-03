using Mdsweep.Domain.Passengers;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<PassengerAggregate>
{
    public void Configure(EntityTypeBuilder<PassengerAggregate> builder)
    {
        builder.ToTable("passengers");
        builder.HasKey(passenger => passenger.Id);
        builder.Property(passenger => passenger.Id).HasColumnName("id");
        builder.Property(passenger => passenger.TenantId).HasColumnName("tenant_id").IsRequired();
        builder
            .Property(passenger => passenger.BrokerMemberId)
            .HasColumnName("broker_member_id")
            .HasMaxLength(100);
        builder.Property(passenger => passenger.FirstName).HasColumnName("first_name").HasMaxLength(200);
        builder.Property(passenger => passenger.LastName).HasColumnName("last_name").HasMaxLength(200);
        builder.HasIndex(passenger => new { passenger.TenantId, passenger.BrokerMemberId }).IsUnique();
    }
}
