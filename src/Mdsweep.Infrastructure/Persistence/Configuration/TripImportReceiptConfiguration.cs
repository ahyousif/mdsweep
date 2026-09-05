using Mdsweep.Domain.TripImports;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TripImportReceiptConfiguration : IEntityTypeConfiguration<TripImportReceipt>
{
    public void Configure(EntityTypeBuilder<TripImportReceipt> builder)
    {
        builder.ToTable("trip_import_receipts");
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.Id).HasColumnName("id");
        builder.Property(receipt => receipt.TenantId).HasColumnName("tenant_id").HasMaxLength(14).IsRequired();
        builder.Property(receipt => receipt.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(receipt => receipt.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsRequired();
        builder.Property(receipt => receipt.Total).HasColumnName("total");
        builder.Property(receipt => receipt.Added).HasColumnName("added");
        builder.Property(receipt => receipt.Updated).HasColumnName("updated");
        builder.Property(receipt => receipt.Unchanged).HasColumnName("unchanged");
        builder.Property(receipt => receipt.ProblemCount).HasColumnName("problem_count");
        builder.Property(receipt => receipt.ImportedAt).HasColumnName("imported_at");
    }
}
