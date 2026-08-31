using Mdsweep.Domain.Tenants;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TenantConfiguration : IEntityTypeConfiguration<TenantAggregate>
{
    public void Configure(EntityTypeBuilder<TenantAggregate> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).HasMaxLength(14).ValueGeneratedNever();
        builder.Property(tenant => tenant.Name).HasMaxLength(200);
        builder.Property(tenant => tenant.KeycloakOrganizationId).HasMaxLength(100);
        builder.HasIndex(tenant => tenant.KeycloakOrganizationId).IsUnique();
    }
}
