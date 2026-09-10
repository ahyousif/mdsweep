using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Tenants;

namespace Mdsweep.Domain.Users;

public sealed class InvitationAggregate : AggregateRoot<Guid>
{
    private InvitationAggregate()
        : base(default) { }

    private InvitationAggregate(
        Guid id,
        string tenantId,
        string email,
        string firstName,
        string lastName,
        string[] roles,
        string status,
        Instant expiresAt,
        Instant? sentAt,
        string? deliveryError,
        Guid? acceptedUserId,
        int version
    )
        : base(id)
    {
        TenantId = tenantId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Roles = roles.ToArray();
        Status = status;
        ExpiresAt = expiresAt;
        SentAt = sentAt;
        DeliveryError = deliveryError;
        AcceptedUserId = acceptedUserId;
        Version = version;
    }

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
        return new InvitationAggregate(
            Guid.CreateVersion7(),
            Guard.Against.NullOrWhiteSpace(tenantId),
            Guard.Against.NullOrWhiteSpace(email),
            Guard.Against.NullOrWhiteSpace(firstName),
            Guard.Against.NullOrWhiteSpace(lastName),
            roles,
            status: "Pending",
            expiresAt: now + Duration.FromDays(7),
            sentAt: null,
            deliveryError: null,
            acceptedUserId: null,
            version: 0
        );
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
