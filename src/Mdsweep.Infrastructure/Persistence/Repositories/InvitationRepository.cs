using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence.Repositories;

public sealed class InvitationRepository(ApplicationDbContext dbContext) : IInvitationRepository
{
    public Task<InvitationAggregate?> GetPendingByTokenHashAsync(string tokenHash, Instant now, CancellationToken ct)
    {
        return dbContext.Invitations.SingleOrDefaultAsync(
            invitation =>
                invitation.TokenHash == tokenHash
                && invitation.Status == InvitationStatus.Pending
                && invitation.ExpiresAt > now,
            ct
        );
    }
}
