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
        builder.OwnsMany(import => import.Items, items =>
        {
            items.ToTable("trip_import_items");
            items.WithOwner().HasForeignKey(item => item.TripImportId);
            items.HasKey(item => item.Id);
            items.Property(item => item.Id).HasColumnName("id");
            items.Property(item => item.TripImportId).HasColumnName("trip_import_id");
            items.Property(item => item.RowNumber).HasColumnName("row_number");
            items.Property(item => item.TripNumber).HasColumnName("trip_number").HasMaxLength(100);
            items.Property(item => item.BrokerMemberId).HasColumnName("broker_member_id").HasMaxLength(100);
            items.Property(item => item.FirstName).HasColumnName("first_name").HasMaxLength(200);
            items.Property(item => item.LastName).HasColumnName("last_name").HasMaxLength(200);
            items.Property(item => item.ServiceDate).HasColumnName("service_date");
            items.Property(item => item.AppointmentTime).HasColumnName("appointment_time").HasColumnType("time");
            items.Property(item => item.PickupAddress).HasColumnName("pickup_address").HasMaxLength(500);
            items.Property(item => item.PickupCity).HasColumnName("pickup_city").HasMaxLength(200);
            items.Property(item => item.DropoffAddress).HasColumnName("dropoff_address").HasMaxLength(500);
            items.Property(item => item.DropoffCity).HasColumnName("dropoff_city").HasMaxLength(200);
            items.Property(item => item.BrokerStatus).HasColumnName("broker_status").HasMaxLength(100);
            items.Property(item => item.IsWillCall).HasColumnName("is_will_call");
            items.Property(item => item.Disposition).HasColumnName("disposition").HasConversion<string>().HasMaxLength(30);
            items.Property(item => item.AppliedTripId).HasColumnName("applied_trip_id");
            items.Property<List<string>>("_messages").HasColumnName("messages").HasColumnType("text[]");
        });
    }
}
