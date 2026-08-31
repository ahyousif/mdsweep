using Mdsweep.Domain.Users;

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
    }
}
