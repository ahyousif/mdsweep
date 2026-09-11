# Use Keycloak for identity

Keycloak will be the MVP identity provider. One MDSweep realm serves each production environment. An earlier design mapped each Tenant to a Keycloak Organization, but that part of this decision is superseded: MDSweep Tenants are independent of Keycloak.

ASP.NET Core is the OpenID Connect client and authentication boundary. It creates a secure HttpOnly MDSweep session cookie after Keycloak authorization-code login. Angular calls same-origin application endpoints and does not receive Keycloak configuration, OAuth access tokens, or refresh tokens.

MDSweep owns tenancy and application authorization. It maps Keycloak `sub` to a local User ID. The server resolves permitted Tenant Memberships from MDSweep, records the selected Tenant ID in its signed application cookie, and rejects client-supplied Tenant IDs. Wolverine detects the selected Tenant globally for conjoined tenancy. This replaces the earlier decision to use ASP.NET Core Identity and local secure cookies. A local development realm and synthetic users are part of the Keycloak integration slice; production realm administration and generic permission modelling are not.

## User management clarification

The User management and Invitations workflow supersedes the earlier suggestion above to express application roles through Keycloak. Keycloak owns authentication, signup, credentials, sessions, and stable external user identity. MDSweep stores and enforces Tenant access and roles in the local Tenant Membership. A User can belong to multiple Tenants, with one or two distinct roles per membership chosen from Administrator, Dispatcher, and Driver. Administrators manage all Users in their Tenant; Dispatchers manage operational Driver assignments, not User accounts. Invitation acceptance creates or reuses the local User and adds the invited Tenant Membership. Active access and history belong to the membership. Invitations are created for the manager's selected Tenant; users choose their working Tenant separately after authentication. Production SMTP setup remains deferred. See [Users and Invitations](../user-management.md) for the workflow and verification details.
