using Mdsweep.Domain.Passengers;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<PassengerAggregate>
{
    public void Configure(EntityTypeBuilder<PassengerAggregate> builder)
    {
        builder.ToTable("Passengers");
        builder.HasKey(passenger => passenger.Id);
        builder.Property(passenger => passenger.FirstName).HasMaxLength(200);
        builder.Property(passenger => passenger.LastName).HasMaxLength(200);
    }
}
