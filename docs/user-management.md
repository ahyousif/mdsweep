# Users and Invitations

Open **Users** in the application sidebar. The page presents one Tenant-scoped list of active Users, disabled Users, and pending Invitations. Use **All**, **Active**, **Invited**, and **Disabled** to filter by status, search by name or email, and select a row to open its detail panel. Search and the selected filter remain in place after an invitation is sent.

Only an Administrator can open this page or manage access. A User can access multiple Tenants through separate Tenant Memberships, each with one or two distinct roles chosen from Administrator, Dispatcher, and Driver. The detail panel supports editing the selected Tenant Membership's display name and roles, disabling or re-enabling that membership, and resending or cancelling a pending Invitation. Global User names and email are read-only here. Administrators cannot deactivate themselves or remove their own Administrator role.

Choose **Invite user** to invite an email address to the active Tenant with a first name, last name, and one or two roles. There is no Tenant selector in the form. A successful request closes the dialog without changing the current search or status filter. A failed request keeps the entered values visible and shows an actionable error.

Invitations store their Tenant, recipient, intended roles, expiry, and current status. The same email may have Invitations from different Tenants, and an existing User can accept access to another Tenant. Email matching is case-insensitive. Creating a new Invitation for an address with an existing pending Invitation in the same Tenant supersedes the previous token. An existing inactive membership must be re-enabled instead of reinvited.

The recipient follows the secure link and signs up or signs in through Keycloak. MDSweep stores only a hash of the one-time token; the Invitation supplies the Tenant, email, roles, and seven-day expiry. Acceptance does not require a selected Tenant or Keycloak's `email_verified` claim. It requires an authenticated identity with an email claim matching the Invitation, then synchronously creates or reuses the local User and adds the Tenant Membership. A used, expired, cancelled, or superseded token cannot grant access. Resending rotates the token and begins a new seven-day expiry period.

Invitations are committed before email delivery. A delivery failure leaves the Invitation pending and visible so an Administrator can resend it. Cancelling an Invitation prevents MDSweep acceptance even if an earlier email link still exists. User and Invitation auditing remains deferred.

**Tenant access** is available from the User menu and Driver landing screen. It lists active memberships; Users with multiple memberships choose which Tenant to open. Switching reloads the application and other open MDSweep tabs so data cached for the previous Tenant is discarded consistently with the shared BFF cookie. The server revalidates the selected membership on each protected request. Deactivation blocks that Tenant on the next request without changing access to other Tenants.

Invitation acceptance does not create a Driver Profile or Trip Assignment. Driver Profile completion, including the MTM Driver Number, remains a separate workflow, and a User cannot be assigned Trips before accepting access.

## Email configuration

Development uses the AppHost's local Mailpit resource. Production uses an external SMTP connection and requires these deployment settings:

- `WEB_BASE_URL`: the application's canonical public HTTP or HTTPS origin, exposed to the AppHost as `Parameters__web_base_url`.
- `SMTP_CONNECTION_STRING`: an SMTP endpoint such as `Endpoint=smtp://smtp.example.test:587`, exposed as `ConnectionStrings__smtp`.
- `SMTP_USERNAME` and `SMTP_PASSWORD`: provider credentials, exposed as the corresponding AppHost parameters.
- `SMTP_FROM`: the invitation sender mailbox, exposed as `Parameters__smtp_from`.

Production SMTP uses STARTTLS. These values belong in the protected GitHub production environment; no production credentials belong in source control. Keycloak realm email configuration is separate and is needed only when Keycloak-managed email workflows such as password reset are introduced.

## Persistence and verification

The current `InitialSchema` migration is the fresh-database baseline and already contains Users, Tenant Memberships, roles, Invitations, and membership display names. It is not an upgrade path for an older production database.

Integration tests use PostgreSQL and cover invitation acceptance, delivery-event behavior, per-membership access changes, and cross-Tenant isolation for listing and cancellation. Angular tests cover the unified list and interaction behavior. `npm run test:e2e` in `src/Mdsweep.Web` runs the synthetic Playwright User-management workflow; install Chromium with `npx playwright install chromium`, or set `PLAYWRIGHT_CHANNEL=chrome` to use an installed Chrome.
