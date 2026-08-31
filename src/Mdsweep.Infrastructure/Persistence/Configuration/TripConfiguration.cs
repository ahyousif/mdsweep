using Mdsweep.Domain.Trips;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TripConfiguration : IEntityTypeConfiguration<TripAggregate>
{
    public void Configure(EntityTypeBuilder<TripAggregate> builder)
    {
        builder.ToTable("trips");
        builder.HasKey(trip => trip.Id);
        builder.Property(trip => trip.TenantId).HasColumnName("tenant_id").HasMaxLength(14).IsRequired();
        builder.Property(trip => trip.BrokerTripNumber).HasMaxLength(100).IsRequired();
        builder.HasIndex(trip => new { trip.TenantId, trip.BrokerTripNumber }).IsUnique();
        builder.HasOne<PassengerAggregate>()
            .WithMany()
            .HasForeignKey(trip => trip.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(trip => trip.ScheduledPickupTime).HasColumnType("time");
        builder.OwnsOne(trip => trip.BrokerFacts, facts =>
        {
            facts.Property(value => value.ServiceDate).HasColumnName("service_date").IsRequired();
            facts.Property(value => value.AppointmentTime).HasColumnName("appointment_time").HasColumnType("time");
            facts.Property(value => value.PickupAddress).HasColumnName("pickup_address").HasMaxLength(500).IsRequired();
            facts.Property(value => value.PickupCity).HasColumnName("pickup_city").HasMaxLength(200).IsRequired();
            facts.Property(value => value.DropoffAddress).HasColumnName("dropoff_address").HasMaxLength(500).IsRequired();
            facts.Property(value => value.DropoffCity).HasColumnName("dropoff_city").HasMaxLength(200).IsRequired();
            facts.Property(value => value.BrokerStatus).HasColumnName("broker_status").HasMaxLength(100);
            facts.Property(value => value.IsWillCall).HasColumnName("is_will_call");
        });
    }
}
