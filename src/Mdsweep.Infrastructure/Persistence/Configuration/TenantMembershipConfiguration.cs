using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TenantId).HasMaxLength(14).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Roles).HasColumnType("text[]").IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();

        builder.HasOne<TenantAggregate>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAggregate>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
