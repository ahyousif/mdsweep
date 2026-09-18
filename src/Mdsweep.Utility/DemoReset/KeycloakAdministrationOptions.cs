namespace Mdsweep.Utility.DemoReset;

public sealed class KeycloakAdministrationOptions
{
    public const string SectionName = "KeycloakAdministration";

    public required string BaseUrl { get; init; }
    public required string AutomationClientId { get; init; }
    public required string AutomationClientSecret { get; init; }
    public required string OidcClientSecret { get; init; }
    public required string DemoAdminPassword { get; init; }
}
