namespace Mdsweep.Application.Common.Keycloak;

public interface IKeycloakAdminClient
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    #region invitations
    Task<Result<KeycloakUserModel>> EnsureUserForInvitationAsync(
        KeycloakInvitationRequest req,
        CancellationToken ct = default
    );
    #endregion
}
