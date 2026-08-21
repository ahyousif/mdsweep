# Use Keycloak for identity

Keycloak will be the MVP identity provider. One MDSweep realm serves each production environment. Each Provider maps to a Keycloak Organization, with organization-scoped roles or groups expressing coarse Dispatcher and Driver membership. Realm-per-Provider is not the default; it remains a later option for exceptional enterprise tenants that require hard IAM isolation.

ASP.NET Core is the OpenID Connect client and authentication boundary. It creates a secure HttpOnly MDSweep session cookie after Keycloak authorization-code login. Angular calls same-origin application endpoints and does not receive Keycloak configuration, OAuth access tokens, or refresh tokens.

MDSweep owns tenancy and application authorization. It maps Keycloak `sub` to a local App User ID, maps every ProviderId to a Keycloak Organization ID, and records ProviderId on every tenant-owned entity. The server resolves permitted Provider context and rejects client-supplied ProviderIds that do not match membership. This replaces the earlier decision to use ASP.NET Core Identity and local secure cookies. A local development realm and synthetic users are part of the Keycloak integration slice; production realm administration and generic permission modelling are not.
