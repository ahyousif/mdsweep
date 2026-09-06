# Use Keycloak for identity

Keycloak will be the MVP identity provider. One MDSweep realm serves each production environment. Each Tenant maps to a Keycloak Organization, with organization-scoped roles or groups expressing coarse Dispatcher and Driver membership. Realm-per-Tenant is not the default; it remains a later option for exceptional enterprise tenants that require hard IAM isolation.

ASP.NET Core is the OpenID Connect client and authentication boundary. It creates a secure HttpOnly MDSweep session cookie after Keycloak authorization-code login. Angular calls same-origin application endpoints and does not receive Keycloak configuration, OAuth access tokens, or refresh tokens.

MDSweep owns tenancy and application authorization. It maps Keycloak `sub` to a local User ID and maps every Tenant ID to a Keycloak Organization ID. The server resolves permitted Tenant memberships, records the selected Tenant ID in its signed application cookie, and rejects client-supplied Tenant IDs. Wolverine detects the selected Tenant globally for conjoined tenancy. This replaces the earlier decision to use ASP.NET Core Identity and local secure cookies. A local development realm and synthetic users are part of the Keycloak integration slice; production realm administration and generic permission modelling are not.

## User management clarification

The User management and Invitations workflow supersedes the earlier suggestion above to express application roles through Keycloak Organization roles or groups. Keycloak owns identity, signup, credentials, and Organization membership; MDSweep stores and enforces application roles in the local Tenant Membership. Each User belongs to one Tenant and holds one or two distinct roles chosen from Administrator, Dispatcher, and Driver. Administrators manage all Users in their Tenant; Dispatchers manage Users whose only role is Driver. Invitation acceptance creates the User and its single Tenant Membership with the invited roles. SMTP setup remains deferred. See [Users and Invitations](../user-management.md) for the workflow and verification details.
