namespace Mdsweep.Application.Common.Keycloak;

public sealed record KeycloakInvitationRequest(string email, string? FirstName, string LastName, string Password);
