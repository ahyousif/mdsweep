using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed record InvitationModel(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string[] Roles,
    string Status,
    Instant ExpiresAt,
    Instant? SentAt,
    string? DeliveryError,
    int Version
)
{
    public static InvitationModel From(InvitationAggregate invitation, Instant now) =>
        new(
            invitation.Id,
            invitation.FirstName,
            invitation.LastName,
            invitation.Email,
            invitation.Roles,
            invitation.Status == "Pending" && invitation.ExpiresAt <= now ? "Expired" : invitation.Status,
            invitation.ExpiresAt,
            invitation.SentAt,
            invitation.DeliveryError,
            invitation.Version
        );
}
