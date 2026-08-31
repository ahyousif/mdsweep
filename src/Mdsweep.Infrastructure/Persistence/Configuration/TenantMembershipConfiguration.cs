using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships");
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.TenantId).HasMaxLength(14);
        builder.Property(membership => membership.Role).HasMaxLength(40);
        builder.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();
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
    }
}
