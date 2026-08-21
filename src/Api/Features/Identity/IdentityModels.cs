namespace Mdsweep.Api.Features.Identity;

public sealed class Provider
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string KeycloakOrganizationId { get; init; }
}

public sealed class AppUser
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string KeycloakSubject { get; init; }
}

public sealed class ProviderMembership
{
    public Guid ProviderId { get; init; }
    public Guid AppUserId { get; init; }
    public required string Role { get; init; }
}
