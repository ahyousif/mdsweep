using Mdsweep.Domain.Users;
using Mdsweep.Domain.Tenants;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class UserConfiguration : IEntityTypeConfiguration<UserAggregate>
{
    public void Configure(EntityTypeBuilder<UserAggregate> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id");
        builder.Property(user => user.FirstName).HasColumnName("first_name").HasMaxLength(200);
        builder.Property(user => user.LastName).HasColumnName("last_name").HasMaxLength(200);
        builder.Property(user => user.KeycloakUserId).HasColumnName("keycloak_user_id").HasMaxLength(200);
        builder.HasIndex(user => user.KeycloakUserId).IsUnique();
        builder.Property(user => user.TenantId).HasColumnName("tenant_id").HasMaxLength(14);
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property<string>("NormalizedEmail").HasColumnName("normalized_email").HasComputedColumnSql("lower(email)", stored: true);
        builder.HasIndex("NormalizedEmail").IsUnique();
        builder.Property(user => user.IsActive).HasColumnName("is_active");
        builder.Property(user => user.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasAlternateKey(user => new { user.Id, user.TenantId });
        builder.HasOne<TenantAggregate>().WithMany().HasForeignKey(user => user.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsMany(user => user.History, history =>
        {
            history.ToTable("user_access_history");
            history.WithOwner().HasForeignKey("user_id");
            history.HasKey(x => x.Id);
            history.Property(x => x.Id).ValueGeneratedNever();
            history.Property(x => x.ActorSubject).HasMaxLength(200);
            history.Property(x => x.Action).HasMaxLength(100);
            history.Property(x => x.Details).HasMaxLength(300);
        });
    }
}
