# Users and Invitations

Open **Users** in the application sidebar. An Administrator can invite and manage all three roles; a Dispatcher sees and manages Users whose only role is Driver. Each User belongs to one Tenant with one or two distinct roles chosen from Administrator, Dispatcher, and Driver. Editing supports names, roles, and active access. Password resets send a Keycloak email; the application never accepts or displays passwords. Names edited here are MDSweep's display names; credentials and the external identity remain owned by Keycloak.

Choose **Invite User** to open the invitation dialog. The dialog reuses the User form with Spartan fields, checkboxes, descriptions, and validation messages, keeps entered details visible when a request fails, and closes after the invitation is saved. Cancel, the close button, or Escape dismisses it when no request is in progress.

Invitations retain their recipient, intended roles, expiry, delivery result, acceptance, and revocation history. A failed email leaves a pending invitation with an actionable error and **Resend**. Duplicate pending invitations and existing User email addresses are rejected case-insensitively. Invitations expire after seven days; a successful resend starts another seven-day period. Keycloak's email link may expire sooner according to its realm action-token setting; resend to obtain a fresh link.

The recipient follows Keycloak's email link, completes signup, and returns to MDSweep to accept. MDSweep verifies the identity's email and membership in the expected Keycloak Organization before granting access. No Driver Profile or Trip Assignment is created by acceptance. Driver Profile completion, including the MTM Driver Number, is a separate later workflow. Users cannot be assigned Trips before acceptance.

Revocation and expiry prevent MDSweep access even if an old Keycloak signup link remains usable. Deactivation blocks authorization on the next protected request, including existing application sessions, and preserves history. Administrators cannot deactivate themselves or remove their own Administrator role. Updates reject stale versions instead of overwriting another manager's changes.

## Email setup is deferred

No SMTP service, credentials, or production realm settings were added. Invitation and password-reset emails fail visibly until Keycloak email delivery is configured. The local Organization redirects recipients to `http://localhost:4200/`; a deployed Organization needs its public application URL as its Redirect URL. The existing Keycloak service account requires Organization and User administration rights. See [Keycloak email configuration](https://www.keycloak.org/docs/latest/server_admin/#_email) when email setup is resumed.

Fresh development databases seed the existing synthetic login with the Administrator role. Existing databases keep their current role; the migration does not promote an existing Dispatcher. Production bootstrap administration remains outside this feature.

## UI components

The Users page, shared invite/edit form, history, and invitation acceptance screen use Spartan components. Search and form controls use Field, Input, and Checkbox; lists use Card and Table with role and status Badges; feedback uses Alert and Spinner; empty results use Empty; history entries use Separator. Page typography uses Spartan's typography utilities. The application shell already uses Sidebar and Dropdown Menu. Feature templates retain semantic HTML and layout utilities for responsive placement.

## Verification

- PostgreSQL HTTP tests cover authorization, Tenant isolation, duplicate emails, delivery retry, acceptance, expiry, revocation, role changes, deactivation, optimistic concurrency, and retained history.
- Migration tests exercise the checked-in `InitialSchema` upgrade with existing synthetic data and reject multiple memberships. The `UserRoles` upgrade preserves existing single roles and pending invitation history; downgrades cannot discard a second role.
- Adapter tests verify the Keycloak 26.2.5 organization invitation and password-reset HTTP contracts without sending real emails.
- `npm run test:e2e` in `src/Mdsweep.Web` runs Playwright browser workflows with synthetic API responses. Install Chromium with `npx playwright install chromium`; alternatively set `PLAYWRIGHT_CHANNEL=chrome` to use an installed Chrome. These browser checks complement the real API/PostgreSQL tests; live mailbox delivery is deferred with SMTP setup.
