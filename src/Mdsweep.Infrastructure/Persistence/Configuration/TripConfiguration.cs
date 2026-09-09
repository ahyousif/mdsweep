using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TripConfiguration : IEntityTypeConfiguration<TripAggregate>
{
    public void Configure(EntityTypeBuilder<TripAggregate> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(trip => trip.Id);

        builder.Property(trip => trip.TenantId).HasMaxLength(14).IsRequired();
        builder.Property(trip => trip.BrokerTripNumber).HasMaxLength(100).IsRequired();

        builder.HasIndex(trip => new { trip.TenantId, trip.BrokerTripNumber }).IsUnique();

        builder.Property(trip => trip.CalculatedPickupTime).HasColumnType("time");
        builder.Property(trip => trip.ManualPickupTime).HasColumnType("time");
        builder.Property(trip => trip.EstimatedTravelMinutes).HasColumnName("estimated_travel_minutes");
        builder.Property(trip => trip.EstimatedDistanceMeters).HasColumnName("estimated_distance_meters");
        builder.Property(trip => trip.ScheduleInputFingerprint).HasMaxLength(64);

        builder
            .HasOne<PassengerAggregate>()
            .WithMany()
            .HasForeignKey(trip => trip.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(trip => trip.Passenger)
            .WithMany()
            .HasForeignKey(trip => trip.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        // I kept explicit names inside OwnsOne intentionally because otherwise EF will
        // tend to incorporate the owned navigation name into those columns. We want a clean flat trips table.
        builder.OwnsOne(
            trip => trip.BrokerData,
            brokerData =>
            {
                brokerData.Property(value => value.ServiceDate).HasColumnName("service_date").IsRequired();
                brokerData.Property(value => value.AppointmentTime).HasColumnName("appointment_time").HasColumnType("time");
                brokerData.Property(value => value.BrokerPickupTime).HasColumnName("broker_pickup_time").HasColumnType("time");
                brokerData.Property(value => value.Direction).HasColumnName("direction");
                brokerData.Property(value => value.IsWillCall).HasColumnName("is_will_call");
                brokerData
                    .Property(value => value.PickupAddress)
                    .HasColumnName("pickup_address")
                    .HasMaxLength(500)
                    .IsRequired();

                brokerData
                    .Property(value => value.PickupCity)
                    .HasColumnName("pickup_city")
                    .HasMaxLength(200)
                    .IsRequired();

                brokerData.Property(value => value.PickupState).HasColumnName("pickup_state").HasMaxLength(100);
                brokerData.Property(value => value.PickupZip).HasColumnName("pickup_zip").HasMaxLength(20);

                brokerData
                    .Property(value => value.DropoffAddress)
                    .HasColumnName("dropoff_address")
                    .HasMaxLength(500)
                    .IsRequired();

                brokerData
                    .Property(value => value.DropoffCity)
                    .HasColumnName("dropoff_city")
                    .HasMaxLength(200)
                    .IsRequired();

                brokerData.Property(value => value.DropoffState).HasColumnName("dropoff_state").HasMaxLength(100);
                brokerData.Property(value => value.DropoffZip).HasColumnName("dropoff_zip").HasMaxLength(20);
                brokerData.Property(value => value.Status).HasColumnName("broker_status").HasMaxLength(100);
                brokerData.Property(value => value.PassengerType).HasColumnName("passenger_type").HasMaxLength(200);
                brokerData.Property(value => value.SpecialNeeds).HasColumnName("special_needs").HasMaxLength(500);
                brokerData.Property(value => value.Cost).HasColumnName("trip_cost").HasPrecision(10, 2);
                brokerData.Property(value => value.Mileage).HasColumnName("trip_mileage").HasPrecision(10, 2);
            }
        );

        builder.Ignore(trip => trip.ScheduledPickupTime);
    }
}
