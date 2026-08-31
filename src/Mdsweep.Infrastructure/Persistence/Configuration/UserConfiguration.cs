using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class UserConfiguration : IEntityTypeConfiguration<UserAggregate>
{
    public void Configure(EntityTypeBuilder<UserAggregate> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.FirstName).HasMaxLength(200);
        builder.Property(user => user.LastName).HasMaxLength(200);
        builder.Property(user => user.KeycloakUserId).HasMaxLength(200);
        builder.HasIndex(user => user.KeycloakUserId).IsUnique();
    }
}
