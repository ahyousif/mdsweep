using Mdsweep.Domain.TripImports;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TripImportConfiguration : IEntityTypeConfiguration<TripImportAggregate>
{
    public void Configure(EntityTypeBuilder<TripImportAggregate> builder)
    {
        builder.ToTable("trip_imports");
        builder.HasKey(import => import.Id);
        builder.Property(import => import.Id).HasColumnName("id");
        builder.Property(import => import.TenantId).HasColumnName("tenant_id").HasMaxLength(14).IsRequired();
        builder.Property(import => import.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(import => import.ContentFingerprint).HasColumnName("content_fingerprint").HasMaxLength(64).IsRequired();
        builder.HasIndex(import => new { import.TenantId, import.ContentFingerprint }).IsUnique()
            .HasFilter("status = 'Applied'");
        builder.Property(import => import.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        builder.Property(import => import.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamp with time zone");
        builder.OwnsMany(import => import.Rows, rows =>
        {
            rows.ToTable("trip_import_rows");
            rows.WithOwner().HasForeignKey(row => row.TripImportId);
            rows.HasKey(row => row.Id);
            rows.Property(row => row.Id).HasColumnName("id");
            rows.Property(row => row.TripImportId).HasColumnName("trip_import_id");
            rows.Property(row => row.RowNumber).HasColumnName("row_number");
            rows.Property(row => row.TripNumber).HasColumnName("trip_number").HasMaxLength(100);
            rows.Property(row => row.BrokerMemberId).HasColumnName("broker_member_id").HasMaxLength(100);
            rows.Property(row => row.FirstName).HasColumnName("first_name").HasMaxLength(200);
            rows.Property(row => row.LastName).HasColumnName("last_name").HasMaxLength(200);
            rows.Property(row => row.ServiceDate).HasColumnName("service_date");
            rows.Property(row => row.AppointmentTime).HasColumnName("appointment_time").HasColumnType("time");
            rows.Property(row => row.PickupAddress).HasColumnName("pickup_address").HasMaxLength(500);
            rows.Property(row => row.PickupCity).HasColumnName("pickup_city").HasMaxLength(200);
            rows.Property(row => row.DropoffAddress).HasColumnName("dropoff_address").HasMaxLength(500);
            rows.Property(row => row.DropoffCity).HasColumnName("dropoff_city").HasMaxLength(200);
            rows.Property(row => row.BrokerStatus).HasColumnName("broker_status").HasMaxLength(100);
            rows.Property(row => row.IsWillCall).HasColumnName("is_will_call");
            rows.Property(row => row.Disposition).HasColumnName("disposition").HasConversion<string>().HasMaxLength(30);
            rows.Property(row => row.AppliedTripId).HasColumnName("applied_trip_id");
            rows.Property<List<string>>("_messages").HasColumnName("messages").HasColumnType("text[]");
        });
    }
}
