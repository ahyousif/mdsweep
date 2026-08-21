# Use Keycloak for identity

Keycloak will be the MVP identity provider. It owns user credentials, sessions, and the `Dispatcher` and `Driver` roles. The Angular PWA uses OpenID Connect authorization code flow with PKCE, and the API validates Keycloak-issued bearer tokens against a configured issuer and audience.

The application stores an authenticated person's immutable Keycloak subject (`sub`) where operational history needs an actor. It does not store passwords or make email addresses durable identity keys. Provider-wide Dispatcher access and assignment-scoped Driver access remain application authorization rules.

This replaces the earlier decision to use ASP.NET Core Identity and local secure cookies. A local development realm and synthetic users are part of the Keycloak integration slice; production realm administration, tenant administration, and generic permission modelling are not.
