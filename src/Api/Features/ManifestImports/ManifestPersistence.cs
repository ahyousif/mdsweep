using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mdsweep.Api.Features.ManifestImports;

internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.TripNumber).IsUnique();
        entity.Property(x => x.TripNumber).HasMaxLength(64);
        entity.Property(x => x.JourneyKey).HasMaxLength(64);
    }
}

internal sealed class ManifestPreviewConfiguration : IEntityTypeConfiguration<ManifestPreview>
{
    public void Configure(EntityTypeBuilder<ManifestPreview> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.RowsJson).HasColumnType("jsonb");
    }
}
