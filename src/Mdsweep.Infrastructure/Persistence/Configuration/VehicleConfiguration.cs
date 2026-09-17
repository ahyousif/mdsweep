using Mdsweep.Domain.Vehicles;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<VehicleAggregate>
{
    public const string VinUniqueIndex = "ix_vehicles_tenant_id_vin";

    public void Configure(EntityTypeBuilder<VehicleAggregate> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.TenantId).IsRequired();
        builder
            .Property(vehicle => vehicle.DisplayLabel)
            .HasMaxLength(VehicleAggregate.MaxDisplayLabelLength)
            .IsRequired();
        builder.Property(vehicle => vehicle.Vin).HasMaxLength(17).IsRequired();
        builder.HasIndex(vehicle => new { vehicle.TenantId, vehicle.Vin }).IsUnique().HasDatabaseName(VinUniqueIndex);
    }
}
