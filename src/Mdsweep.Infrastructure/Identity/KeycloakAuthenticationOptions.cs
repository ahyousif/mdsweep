namespace Mdsweep.Infrastructure.Identity;

public sealed class KeycloakAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public required string Authority { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}
