namespace Mdsweep.Domain.Users;

public interface IInvitationRepository
{
    Task<InvitationAggregate?> GetPendingByTokenHashAsync(string tokenHash, Instant now, CancellationToken ct);
}
