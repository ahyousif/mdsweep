using Mdsweep.Domain.Tenants;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TenantConfiguration : IEntityTypeConfiguration<TenantAggregate>
{
    public void Configure(EntityTypeBuilder<TenantAggregate> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).HasColumnName("id").HasMaxLength(14).ValueGeneratedNever();
        builder.Property(tenant => tenant.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(tenant => tenant.KeycloakOrganizationId).HasColumnName("keycloak_organization_id").HasMaxLength(100);
        builder.HasIndex(tenant => tenant.KeycloakOrganizationId).IsUnique();
    }
}
