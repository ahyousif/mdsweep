# Users and Invitations

The Users page has separate **Users** and **Invitations** tabs, with Users selected by default. Search filters both lists. **Invite User** opens the invitation dialog from either tab. After an invitation is saved, the Invitations tab opens and search clears so its delivery status and Resend, Revoke, and History actions are visible.

Open **Users** in the application sidebar. An Administrator can invite and manage all three roles; only Administrators manage User accounts. A User can access multiple Tenants through separate memberships, each with one or two distinct roles chosen from Administrator, Dispatcher, and Driver. Editing supports Tenant Membership display names, roles, and active access. Password resets send a Keycloak email; the application never accepts or displays passwords. Global User names and email are read-only here. Administrators edit Tenant Membership display names; self-service profile editing is deferred. Roles, active access, version checks, and access history are specific to the selected Tenant.

Choose **Invite User** to open the invitation dialog for the current Tenant. There is no Tenant selection in this form. The dialog reuses the User form with Spartan fields, checkboxes, descriptions, and validation messages, keeps entered details visible when a request fails, and closes after the invitation is saved. Cancel, the close button, or Escape dismisses it when no request is in progress.

Invitations retain their recipient, intended roles, expiry, delivery result, acceptance, and revocation history. A failed email leaves a pending invitation with an actionable error and **Resend**. Duplicate pending invitations and existing memberships are rejected within the current Tenant. Email matching is case-insensitive. The same email may receive invitations from multiple Tenants, and an existing User can accept another Tenant's invitation with the same identity. An inactive membership must be reactivated instead of reinvited. Invitations expire after seven days; a successful resend starts another seven-day period. Keycloak's email link may expire sooner according to its realm action-token setting; resend to obtain a fresh link.

The recipient follows Keycloak's email link, completes signup or signs in to their existing identity, and returns to MDSweep to accept. **Tenants and Invitations** is available from the User menu and Driver landing screen. It lists pending invitations and existing active memberships; users with multiple memberships choose which Tenant to open. Switching reloads the application and other open MDSweep tabs so the previous Tenant's cached responses and forms are discarded consistently with the shared BFF cookie. The session endpoint returns the selected Tenant from the signed BFF cookie and revalidates active memberships on the server. MDSweep verifies the identity's email and membership in the expected Keycloak Organization before granting access. No Driver Profile or Trip Assignment is created by acceptance. Driver Profile completion, including the MTM Driver Number, is a separate later workflow. Users cannot be assigned Trips before acceptance.

Revocation and expiry prevent MDSweep access even if an old Keycloak signup link remains usable. Deactivation blocks authorization for that Tenant on the next protected request, including existing application sessions, and preserves that membership's history. Access in other Tenants is unchanged. Replaying an accepted invitation never restores deactivated access. Administrators cannot deactivate themselves or remove their own Administrator role. Updates reject stale versions instead of overwriting another manager's changes.

## Email setup is deferred

No SMTP service, credentials, or production realm settings were added. Invitation and password-reset emails fail visibly until Keycloak email delivery is configured. The local Organization redirects recipients to `http://localhost:4200/`; a deployed Organization needs its public application URL as its Redirect URL. The existing Keycloak service account requires Organization and User administration rights. See [Keycloak email configuration](https://www.keycloak.org/docs/latest/server_admin/#_email) when email setup is resumed.

Fresh development databases seed the existing synthetic login with the Administrator role. Existing databases keep their current role; the migration does not promote an existing Dispatcher. Production bootstrap administration remains outside this feature.

## UI components

The Users page, shared invite/edit form, history, and invitation acceptance screen use Spartan components. Search and form controls use Field, Input, and Checkbox; lists use Card and Table with role and status Badges; feedback uses Alert and Spinner; empty results use Empty; history entries use Separator. Page typography uses Spartan's typography utilities. The application shell already uses Sidebar and Dropdown Menu. Feature templates retain semantic HTML and layout utilities for responsive placement.

## Verification

- PostgreSQL HTTP tests cover authorization, Tenant isolation, duplicate emails, delivery retry, acceptance, expiry, revocation, role changes, deactivation, optimistic concurrency, and retained history.
- Migration tests exercise forward upgrades from `InitialSchema`, through the Trip Import and scheduling migrations, to `UserManagementAndInvitations` and `UserRoles`. The original feature migrations create multiple-Tenant access directly, with active flags, versions, and history owned by memberships. PostgreSQL enforces one membership per User/Tenant pair and one pending invitation per Tenant/email pair. The role upgrade preserves memberships, access state, and history; a downgrade cannot discard a second role.
- Adapter tests verify the Keycloak 26.2.5 organization invitation and password-reset HTTP contracts without sending real emails.
- `npm run test:e2e` in `src/Mdsweep.Web` runs Playwright browser workflows with synthetic API responses. Install Chromium with `npx playwright install chromium`; alternatively set `PLAYWRIGHT_CHANNEL=chrome` to use an installed Chrome. These browser checks complement the real API/PostgreSQL tests; live mailbox delivery is deferred with SMTP setup.

Tenant Membership display names are independent of the global User profile and other Tenants. Memberships without an override display the global name. The existing, unapplied UserManagementAndInvitations migration includes the display-name column; no additional migration is needed.

Every User requires an email address. User creation rejects missing or blank email, and the database requires a non-null email. The access migration is a fresh-database baseline; it does not invent email addresses for older records.
