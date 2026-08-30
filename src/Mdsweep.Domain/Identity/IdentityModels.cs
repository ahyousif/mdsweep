namespace Mdsweep.Domain.Identity;

public sealed class Provider
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; init; }
    public required string KeycloakOrganizationId { get; init; }
}

public sealed class AppUser
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string KeycloakSubject { get; init; }
}

public sealed class ProviderMembership
{
    public Guid ProviderId { get; init; }
    public Guid AppUserId { get; init; }
    public required string Role { get; init; }
}
