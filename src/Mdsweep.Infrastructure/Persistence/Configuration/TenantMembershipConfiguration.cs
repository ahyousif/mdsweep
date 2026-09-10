using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable(
            "tenant_memberships",
            table =>
                table.HasCheckConstraint(
                    "ck_tenant_memberships_roles",
                    "cardinality(roles) BETWEEN 1 AND 2 AND roles <@ ARRAY['Administrator','Dispatcher','Driver']::text[] AND array_position(roles, NULL) IS NULL AND (cardinality(roles) = 1 OR roles[1] <> roles[2])"
                )
        );
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Id).HasColumnName("id");
        builder.Property(membership => membership.TenantId).HasColumnName("tenant_id").HasMaxLength(14);
        builder.Property(membership => membership.UserId).HasColumnName("user_id");
        builder.Property(membership => membership.Roles).HasColumnName("roles").HasColumnType("text[]");
        builder.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();
        builder.Property(membership => membership.DisplayName).HasColumnName("display_name").HasMaxLength(401);
        builder.Property(membership => membership.IsActive).HasColumnName("is_active");
        builder.Property(membership => membership.Version).HasColumnName("version").IsConcurrencyToken();
        builder
            .HasOne<TenantAggregate>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<UserAggregate>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.OwnsMany(
            membership => membership.History,
            history =>
            {
                history.ToTable("user_access_history");
                history.WithOwner().HasForeignKey("membership_id");
                history.HasKey(x => x.Id);
                history.Property(x => x.Id).ValueGeneratedNever();
                history.Property(x => x.ActorSubject).HasMaxLength(200);
                history.Property(x => x.Action).HasMaxLength(100);
                history.Property(x => x.Details).HasMaxLength(300);
            }
        );
    }
}
