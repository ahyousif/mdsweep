using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Users.Events;

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
        string tokenHash,
        InvitationStatus status,
        Instant expiresAt,
        Instant? acceptedAt
    )
        : base(id)
    {
        TenantId = tenantId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Roles = roles;
        TokenHash = tokenHash;
        Status = status;
        ExpiresAt = expiresAt;
        AcceptedAt = acceptedAt;
    }

    public string TenantId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string[] Roles { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public InvitationStatus Status { get; private set; }
    public Instant ExpiresAt { get; private set; }
    public Instant? AcceptedAt { get; private set; }

    public static InvitationAggregate Create(
        string tenantId,
        string email,
        string firstName,
        string lastName,
        string[] roles,
        string token,
        string tokenHash,
        Instant expiresAt
    )
    {
        var invitation = new InvitationAggregate(
            Guid.CreateVersion7(),
            tenantId,
            Guard.Against.NullOrWhiteSpace(email),
            Guard.Against.NullOrWhiteSpace(firstName),
            Guard.Against.NullOrWhiteSpace(lastName),
            roles,
            Guard.Against.NullOrWhiteSpace(tokenHash),
            status: InvitationStatus.Pending,
            expiresAt: expiresAt,
            acceptedAt: null
        );

        invitation.AddDomainEvent(
            new InvitationCreatedDomainEvent(
                invitation.Id,
                invitation.Email,
                invitation.FirstName,
                token,
                invitation.ExpiresAt
            )
        );

        return invitation;
    }

    public void Accept(Instant acceptedAt)
    {
        Guard.Against.Invalid(IsExpired(acceptedAt));
        Guard.Against.Invalid(Status is not InvitationStatus.Pending);

        Status = InvitationStatus.Accepted;
        AcceptedAt = acceptedAt;
    }

    public void Resend(string token, string tokenHash, Instant expiresAt)
    {
        Guard.Against.Invalid(Status is not InvitationStatus.Pending);

        TokenHash = Guard.Against.NullOrWhiteSpace(tokenHash);
        ExpiresAt = expiresAt;

        AddDomainEvent(new InvitationCreatedDomainEvent(Id, Email, FirstName, token, ExpiresAt));
    }

    public void Cancel()
    {
        if (Status is not InvitationStatus.Pending)
        {
            return;
        }

        Status = InvitationStatus.Cancelled;

        AddDomainEvent(new InvitationCancelledDomainEvent(Id));
    }

    public bool IsExpired(Instant now) => now >= ExpiresAt && AcceptedAt is null;
}
