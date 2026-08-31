namespace Mdsweep.Infrastructure.Identity;

public sealed class KeycloakAdministrationOptions
{
    public const string SectionName = "KeycloakAdministration";

    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}
