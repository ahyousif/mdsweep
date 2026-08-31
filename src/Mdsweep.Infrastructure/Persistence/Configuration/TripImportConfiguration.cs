using Mdsweep.Domain.TripImports;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TripImportConfiguration : IEntityTypeConfiguration<TripImportAggregate>
{
    public void Configure(EntityTypeBuilder<TripImportAggregate> builder)
    {
        builder.ToTable("trip_imports");
        builder.HasKey(import => import.Id);
        builder.Property(import => import.TenantId).HasColumnName("tenant_id").HasMaxLength(14).IsRequired();
        builder.Property(import => import.FileName).HasMaxLength(255).IsRequired();
        builder.Property(import => import.ContentFingerprint).HasMaxLength(64).IsRequired();
        builder.HasIndex(import => new { import.TenantId, import.ContentFingerprint }).IsUnique();
        builder.Property(import => import.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(import => import.AppliedAt).HasColumnType("timestamp with time zone");
        builder.OwnsMany(import => import.Rows, rows =>
        {
            rows.ToTable("trip_import_rows");
            rows.WithOwner().HasForeignKey(row => row.TripImportId);
            rows.HasKey(row => row.Id);
            rows.Property(row => row.TripNumber).HasMaxLength(100);
            rows.Property(row => row.BrokerMemberId).HasMaxLength(100);
            rows.Property(row => row.FirstName).HasMaxLength(200);
            rows.Property(row => row.LastName).HasMaxLength(200);
            rows.Property(row => row.AppointmentTime).HasColumnType("time");
            rows.Property(row => row.PickupAddress).HasMaxLength(500);
            rows.Property(row => row.PickupCity).HasMaxLength(200);
            rows.Property(row => row.DropoffAddress).HasMaxLength(500);
            rows.Property(row => row.DropoffCity).HasMaxLength(200);
            rows.Property(row => row.BrokerStatus).HasMaxLength(100);
            rows.Property(row => row.Disposition).HasConversion<string>().HasMaxLength(30);
            rows.Property(row => row.Messages)
                .HasColumnType("text[]")
                .HasConversion(
                    messages => messages.ToArray(),
                    messages => messages.ToList()
                );
        });
    }
}
