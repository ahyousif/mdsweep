using Mdsweep.Domain.Passengers;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<PassengerAggregate>
{
    public void Configure(EntityTypeBuilder<PassengerAggregate> builder)
    {
        builder.ToTable("passengers");
        builder.HasKey(passenger => passenger.Id);
        builder.Property(passenger => passenger.Id);
        builder.Property(passenger => passenger.TenantId).IsRequired();
        builder.Property(passenger => passenger.BrokerMemberId).HasMaxLength(100);
        builder.Property(passenger => passenger.FirstName).HasMaxLength(200);
        builder.Property(passenger => passenger.LastName).HasMaxLength(200);
        builder.Property(passenger => passenger.DateOfBirth);
        builder.Property(passenger => passenger.PhoneNumber).HasMaxLength(50);
        builder.Property(passenger => passenger.AlternatePhoneNumber).HasMaxLength(50);
        builder.Property(passenger => passenger.PassengerType).HasMaxLength(200);
        builder.Property(passenger => passenger.SpecialNeeds).HasMaxLength(500);
        builder.Property(passenger => passenger.Notes).HasMaxLength(2000);
        builder.Property(passenger => passenger.IsActive).HasDefaultValue(true);
        builder.HasIndex(passenger => new { passenger.TenantId, passenger.BrokerMemberId }).IsUnique();
    }
}
