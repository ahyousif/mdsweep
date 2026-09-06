using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships", table => table.HasCheckConstraint("ck_tenant_memberships_roles",
            "cardinality(roles) BETWEEN 1 AND 2 AND roles <@ ARRAY['Administrator','Dispatcher','Driver']::text[] AND array_position(roles, NULL) IS NULL AND (cardinality(roles) = 1 OR roles[1] <> roles[2])"));
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Id).HasColumnName("id");
        builder.Property(membership => membership.TenantId).HasColumnName("tenant_id").HasMaxLength(14);
        builder.Property(membership => membership.UserId).HasColumnName("user_id");
        builder.Property(membership => membership.Roles).HasColumnName("roles").HasColumnType("text[]");
        builder.HasIndex(membership => membership.UserId).IsUnique();
        builder
            .HasOne<TenantAggregate>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAggregate>()
            .WithMany()
            .HasForeignKey(membership => new { membership.UserId, membership.TenantId })
            .HasPrincipalKey(user => new { user.Id, user.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
