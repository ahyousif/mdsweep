using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mdsweep.Api.Features.ManifestImports;

namespace Mdsweep.Api.Features.Dispatch;

internal sealed class TripScheduleConfiguration : IEntityTypeConfiguration<TripSchedule>
{
    public void Configure(EntityTypeBuilder<TripSchedule> entity)
    {
        entity.HasKey(x => x.TripId);
        entity.HasOne<Trip>().WithOne().HasForeignKey<TripSchedule>(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ScheduledPickupTimeChangeConfiguration : IEntityTypeConfiguration<ScheduledPickupTimeChange>
{
    public void Configure(EntityTypeBuilder<ScheduledPickupTimeChange> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.TripId, x.ChangedAt });
        entity.Property(x => x.ChangedBy).HasMaxLength(450);
        entity.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
    }
}
