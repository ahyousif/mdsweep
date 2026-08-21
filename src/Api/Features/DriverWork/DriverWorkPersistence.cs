using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mdsweep.Api.Features.Dispatch;
using Mdsweep.Api.Features.ManifestImports;

namespace Mdsweep.Api.Features.DriverWork;

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
        entity.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Driver>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
    }
}
