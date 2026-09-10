namespace Mdsweep.Application.Common.Keycloak;

public sealed record KeycloakUserModel
{
    public string? Id { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool? EmailVerified { get; init; }
    public bool? Enabled { get; init; }
}
