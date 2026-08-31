using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.ManifestImports;

namespace Mdsweep.Infrastructure.DriverWork;

internal sealed class DriverTripEventConfiguration : IEntityTypeConfiguration<DriverTripEvent>
{
    public void Configure(EntityTypeBuilder<DriverTripEvent> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.TripId, x.ReceivedAt });
        entity.HasIndex(x => new { x.TripId, x.DeviceCapturedAt }).IsUnique();
        entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        entity.Property(x => x.OutcomeReason).HasMaxLength(80);
        entity.Property(x => x.Note).HasMaxLength(1000);
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
    }
}

internal sealed class DriverTripEventCorrectionConfiguration
    : IEntityTypeConfiguration<DriverTripEventCorrection>
{
    public void Configure(EntityTypeBuilder<DriverTripEventCorrection> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.DriverTripEventId, x.ReceivedAt });
        entity.Property(x => x.Reason).HasMaxLength(500);
        entity
            .HasOne<DriverTripEvent>()
            .WithMany()
            .HasForeignKey(x => x.DriverTripEventId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne<Driver>()
            .WithMany()
            .HasForeignKey(x => x.CorrectedByDriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DriverTripSyncConflictConfiguration
    : IEntityTypeConfiguration<DriverTripSyncConflict>
{
    public void Configure(EntityTypeBuilder<DriverTripSyncConflict> entity)
    {
        entity.HasKey(x => x.Id);
        entity
            .HasIndex(x => new
            {
                x.ProviderId,
                x.DriverId,
                x.ActionId,
            })
            .IsUnique();
        entity.HasIndex(x => new { x.ProviderId, x.ReceivedAt });
        entity.Property(x => x.TripNumber).HasMaxLength(64);
        entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        entity.Property(x => x.Reason).HasMaxLength(500);
        entity.Property(x => x.OutcomeReason).HasMaxLength(80);
        entity.Property(x => x.Note).HasMaxLength(1000);
        entity
            .HasOne<Driver>()
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
