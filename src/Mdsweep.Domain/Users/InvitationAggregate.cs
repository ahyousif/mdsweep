using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Tenants;

namespace Mdsweep.Domain.Users;

public sealed class InvitationAggregate : AggregateRoot<Guid>
{
    private InvitationAggregate()
        : base(default) { }

    private InvitationAggregate(Guid id)
        : base(id) { }

    public string TenantId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string[] Roles { get; private set; } = null!;
    public string Status { get; private set; } = "Pending";
    public Instant ExpiresAt { get; private set; }
    public Instant? SentAt { get; private set; }
    public string? DeliveryError { get; private set; }
    public Guid? AcceptedUserId { get; private set; }
    public int Version { get; private set; }

    public static InvitationAggregate Create(
        string tenantId,
        string email,
        string firstName,
        string lastName,
        string[] roles,
        Instant now
    )
    {
        Guard.Against.Invalid(!TenantMembership.AreValidRoles(roles), "Select one or two distinct roles.");
        var invitation = new InvitationAggregate(Guid.CreateVersion7())
        {
            TenantId = Guard.Against.NullOrWhiteSpace(tenantId),
            Email = Guard.Against.NullOrWhiteSpace(email),
            FirstName = Guard.Against.NullOrWhiteSpace(firstName),
            LastName = Guard.Against.NullOrWhiteSpace(lastName),
            Roles = roles.ToArray(),
            ExpiresAt = now + Duration.FromDays(7),
        };
        return invitation;
    }

    public void RecordDelivery(Instant now, string? error)
    {
        if (Status != "Pending")
            throw new InvalidOperationException("This invitation is no longer pending.");
        DeliveryError = error;
        if (error is null)
        {
            SentAt = now;
            ExpiresAt = now + Duration.FromDays(7);
        }
        Version++;
    }

    public void Revoke()
    {
        if (Status == "Revoked")
            return;
        if (Status != "Pending")
            throw new InvalidOperationException("An accepted invitation cannot be revoked.");
        Status = "Revoked";
        Version++;
    }

    public void Accept(Guid userId, Instant now)
    {
        if (Status != "Pending" || ExpiresAt <= now)
            throw new InvalidOperationException("This invitation has expired or is no longer available.");
        Status = "Accepted";
        AcceptedUserId = userId;
        Version++;
    }
}
