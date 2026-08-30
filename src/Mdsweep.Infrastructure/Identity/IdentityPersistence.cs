using Mdsweep.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mdsweep.Infrastructure.Identity;

internal sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.KeycloakOrganizationId).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.KeycloakOrganizationId).HasMaxLength(100);
    }
}

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.KeycloakSubject).IsUnique();
        entity.Property(x => x.KeycloakSubject).HasMaxLength(200);
    }
}

internal sealed class ProviderMembershipConfiguration : IEntityTypeConfiguration<ProviderMembership>
{
    public void Configure(EntityTypeBuilder<ProviderMembership> entity)
    {
        entity.HasKey(x => new { x.ProviderId, x.AppUserId });
        entity.Property(x => x.Role).HasMaxLength(40);
        entity
            .HasOne<Provider>()
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
