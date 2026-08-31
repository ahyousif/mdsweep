using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.ManifestImports;

namespace Mdsweep.Infrastructure.Dispatch;

internal sealed class TripScheduleConfiguration : IEntityTypeConfiguration<TripSchedule>
{
    public void Configure(EntityTypeBuilder<TripSchedule> entity)
    {
        entity.HasKey(x => x.TripId);
        entity
            .HasOne<Trip>()
            .WithOne()
            .HasForeignKey<TripSchedule>(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ScheduledPickupTimeChangeConfiguration
    : IEntityTypeConfiguration<ScheduledPickupTimeChange>
{
    public void Configure(EntityTypeBuilder<ScheduledPickupTimeChange> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Sequence).ValueGeneratedOnAdd();
        entity.HasIndex(x => x.Sequence).IsUnique();
        entity.HasIndex(x => new { x.TripId, x.ChangedAt });
        entity.Property(x => x.ChangedBy).HasMaxLength(450);
        entity
            .HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.TenantId, x.MtmDriverNumber }).IsUnique();
        entity.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
        entity.Property(x => x.DisplayName).HasMaxLength(200);
        entity.Property(x => x.MtmDriverNumber).HasMaxLength(64);
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.TenantId, x.Vin }).IsUnique();
        entity.Property(x => x.DisplayName).HasMaxLength(200);
        entity.Property(x => x.Vin).HasMaxLength(32);
    }
}

internal sealed class TripAssignmentConfiguration : IEntityTypeConfiguration<TripAssignment>
{
    public void Configure(EntityTypeBuilder<TripAssignment> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.TripId).HasFilter("\"SupersededAt\" IS NULL").IsUnique();
        entity
            .HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne<Driver>()
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
