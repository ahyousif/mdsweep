using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TripConfiguration : IEntityTypeConfiguration<TripAggregate>
{
    public void Configure(EntityTypeBuilder<TripAggregate> builder)
    {
        builder.ToTable("trips");
        builder.HasKey(trip => trip.Id);
        builder.Property(trip => trip.Id).HasColumnName("id");
        builder.Property(trip => trip.TenantId).HasColumnName("tenant_id").HasMaxLength(14).IsRequired();
        builder.Property(trip => trip.PassengerId).HasColumnName("passenger_id");
        builder
            .Property(trip => trip.BrokerTripNumber)
            .HasColumnName("broker_trip_number")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(trip => new { trip.TenantId, trip.BrokerTripNumber }).IsUnique();
        builder
            .HasOne(trip => trip.Passenger)
            .WithMany()
            .HasForeignKey(trip => trip.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(trip => trip.ScheduledPickupTime).HasColumnName("scheduled_pickup_time").HasColumnType("time");
        builder.OwnsOne(
            trip => trip.BrokerData,
            facts =>
            {
                facts.Property(value => value.ServiceDate).HasColumnName("service_date").IsRequired();
                facts.Property(value => value.AppointmentTime).HasColumnName("appointment_time").HasColumnType("time");
                facts
                    .Property(value => value.PickupAddress)
                    .HasColumnName("pickup_address")
                    .HasMaxLength(500)
                    .IsRequired();
                facts.Property(value => value.PickupCity).HasColumnName("pickup_city").HasMaxLength(200).IsRequired();
                facts
                    .Property(value => value.DropoffAddress)
                    .HasColumnName("dropoff_address")
                    .HasMaxLength(500)
                    .IsRequired();
                facts.Property(value => value.DropoffCity).HasColumnName("dropoff_city").HasMaxLength(200).IsRequired();
                facts.Property(value => value.BrokerStatus).HasColumnName("broker_status").HasMaxLength(100);
                facts.Property(value => value.IsWillCall).HasColumnName("is_will_call");
                facts
                    .Property(value => value.MobilityRequirement)
                    .HasColumnName("mobility_requirement")
                    .HasConversion<string>()
                    .HasMaxLength(60)
                    .IsRequired();
                facts
                    .Property(value => value.RawImportedPassengerType)
                    .HasColumnName("raw_imported_passenger_type")
                    .HasMaxLength(200);
                facts.Property(value => value.TripCost).HasColumnName("trip_cost").HasPrecision(10, 2);
                facts.Property(value => value.TripMileage).HasColumnName("trip_mileage").HasPrecision(10, 2);
                facts.Ignore(value => value.RequiredVehicleCapability);
            }
        );
    }
}
