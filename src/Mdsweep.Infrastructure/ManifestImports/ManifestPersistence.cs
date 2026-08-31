using Mdsweep.Domain.ManifestImports;
using Mdsweep.Domain.Passengers;

namespace Mdsweep.Infrastructure.ManifestImports;

internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.TenantId, x.TripNumber }).IsUnique();
        entity.HasIndex(x => x.TenantId);
        entity.Property(x => x.TripNumber).HasMaxLength(64);
        entity.Property(x => x.JourneyKey).HasMaxLength(64);
        entity
            .HasOne<PassengerAggregate>()
            .WithMany()
            .HasForeignKey(x => x.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TripBrokerImportConfiguration : IEntityTypeConfiguration<TripBrokerImport>
{
    public void Configure(EntityTypeBuilder<TripBrokerImport> entity)
    {
        entity.HasKey(x => x.Id);
        entity
            .HasIndex(x => new
            {
                x.TenantId,
                x.ManifestReceiptId,
                x.TripNumber,
            })
            .IsUnique();
        entity.Property(x => x.TripNumber).HasMaxLength(64);
        entity.Property(x => x.BrokerMemberId).HasMaxLength(100);
        entity
            .HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne<ManifestReceipt>()
            .WithMany()
            .HasForeignKey(x => x.ManifestReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ManifestReceiptConfiguration : IEntityTypeConfiguration<ManifestReceipt>
{
    public void Configure(EntityTypeBuilder<ManifestReceipt> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.TenantId);
        entity.Property(x => x.RowsJson).HasColumnType("jsonb");
    }
}
