using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence.Configuration;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<InvitationAggregate>
{
    public void Configure(EntityTypeBuilder<InvitationAggregate> builder)
    {
        builder.ToTable("invitations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TenantId).HasMaxLength(14);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique().HasFilter("\"Status\" = 'Pending'");
        builder.Property(x => x.FirstName).HasMaxLength(200);
        builder.Property(x => x.LastName).HasMaxLength(200);
        builder.Property(x => x.Roles).HasColumnType("text[]");
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.DeliveryError).HasMaxLength(300);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.TenantId);
        builder.HasOne<TenantAggregate>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsMany(
            x => x.History,
            history =>
            {
                history.ToTable("invitation_history");
                history.WithOwner().HasForeignKey("invitation_id");
                history.HasKey(x => x.Id);
                history.Property(x => x.Id).ValueGeneratedNever();
                history.Property(x => x.ActorSubject).HasMaxLength(200);
                history.Property(x => x.Action).HasMaxLength(100);
                history.Property(x => x.Details).HasMaxLength(300);
            }
        );
    }
}
