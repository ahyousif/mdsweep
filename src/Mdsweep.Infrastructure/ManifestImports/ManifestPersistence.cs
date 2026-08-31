using Mdsweep.Domain.ManifestImports;

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
        entity.Property(x => x.PassengerPhone).HasMaxLength(32);
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
                x.ManifestPreviewId,
                x.TripNumber,
            })
            .IsUnique();
        entity.Property(x => x.TripNumber).HasMaxLength(64);
        entity
            .HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne<ManifestPreview>()
            .WithMany()
            .HasForeignKey(x => x.ManifestPreviewId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ManifestPreviewConfiguration : IEntityTypeConfiguration<ManifestPreview>
{
    public void Configure(EntityTypeBuilder<ManifestPreview> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.TenantId);
        entity.Property(x => x.RowsJson).HasColumnType("jsonb");
    }
}
