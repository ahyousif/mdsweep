# MVP Architecture

## Shape

Build a modular monolith with a slim vertical-slice structure. The deployed product has an Angular PWA, one ASP.NET Core application, and PostgreSQL. .NET Aspire composes the development environment and deployable resources.

### Delivery state

The system has a shared Azure production-shaped environment for deployment validation, but it is not approved for patient-linked data. Local and deployed data remain synthetic. The checked-in EF Core migration is the schema baseline for a fresh database. A deployment-readiness decision is required before non-synthetic data is introduced; it must define the migration procedure, backups and restore test, Keycloak realm administration, and data-safety approval.

The application replaces the provider's legacy operations site. MTM integration at both ends is file-based for the MVP:

```text
MTM manifest → Trips → billing file → MTM portal
```

The user downloads the manifest and uploads the billing file manually. Wolverine is used only as an in-process command/query dispatcher, HTTP endpoint model, and lightweight EF Core unit of work. Durable queues, inboxes, outboxes, scheduled jobs, browser automation, and an automation worker enter the architecture only when an authorized durable workflow exists.

## Modules

Each module presents a small interface through its HTTP endpoints and application commands. Business rules, persistence, validation, and history remain local to the module that owns the behavior.

### Passengers

**Interface:** create, find, maintain, and inspect a Passenger's Trip history.

Passengers owns Tenant-scoped Passenger identity and broker-specific member identifiers. A Passenger may exist before any Manifest or Trip. Trips references a Passenger; it does not own Passenger identity. Manifest adapters reconcile broker-provided Passenger details through this module without overwriting Tenant-owned information.

### Trips

**Interface:** review and accept a Manifest, plan and assign Trips, record Trip outcomes and actual timestamps, review and close Trips, and prepare a billing file.

Trips is one deep module organized internally by Manifest intake, planning, performance, review, and billing. It owns Trip identity, Journey relationships, Tenant planning decisions, Assignment history, actual timestamps, outcomes, corrections, closure, billing readiness, and Operational History.

CSV/XLSX readers translate external Manifests into reviewed input without owning Passenger or Trip state. Applying the same source repeatedly must not duplicate Passenger or Trip records, erase Tenant-owned changes, or discard earlier broker-provided details.

TripImports is a sibling feature to Trips. It owns the retained preview and application lifecycle for one uploaded broker file; Trips owns the resulting Trip aggregate and its operational state.

The Dispatcher and Driver experiences are separate HTTP and web adapters over Trips. Driver-facing queries disclose only assigned Trips, and Driver actions remain authorized against the active Assignment. Vehicle management and Vehicle Assignment are outside the MVP.

Billing-file writers translate billing-ready Trip data into the MTM workbook and retain the generated Billing Batch. The Dispatcher continues the manual MTM Link review and submission workflow.

The exact file implementation generates the ten-column `.xlsx` Claims Sheet documented in `docs/research/mtm-bulk-claim-upload.md`. Production compatibility remains gated on a bounded synthetic portal trial for the unresolved validation, duplicate, correction, signature-document, and partial-failure behavior recorded in the research note.

### Access

Access owns Users, Tenant Memberships, and role authorization. Keycloak owns external identities, credentials, and sessions. One MDSweep Keycloak realm serves each production environment; a Tenant maps to a Keycloak Organization, not to a dedicated realm by default. A dedicated realm is reserved for an exceptional enterprise tenant that requires hard IAM isolation.

ASP.NET Core is the OpenID Connect client and backend-for-frontend: it establishes the HttpOnly application cookie after authorization code authentication with Keycloak. Angular calls same-origin application endpoints and never receives or manages Keycloak tokens. The API maps Keycloak's immutable `sub` to the local User ID, returns allowed Tenant memberships from `/api/auth/me`, and accepts a membership-verified selection at `/api/auth/tenant-context`. The selected Tenant ID is stored in the signed application cookie as `mdsweep_tenant_id`; Wolverine detects it globally for conjoined tenancy. Reusable authorization policies verify the User's membership and role for protected resources. The application never trusts a client-supplied Tenant ID.

## Seams and adapters

### Travel-time estimation

Trips owns a narrow travel-time interface: estimate travel duration for a pickup, destination, and relevant departure context. Its first production adapter uses configured route or city presets. A future mapping adapter may replace it after contractual and privacy review. Tests use a deterministic adapter.

The interface returns an estimate or an actionable failure. It does not assign Drivers or decide the Scheduled Pickup Time; Trips combines the estimate with configured arrival and loading buffers.

### Time

Timestamp-sensitive Trips behavior receives time from an injected clock. Production uses system time and tests use a controlled clock so offline receipt and event ordering are deterministic. Correction authorization and timing remain unresolved product policy.

Application handlers do not receive `ApplicationDbContext`. Aggregate writes use the common `IRepository`, which Wolverine maps to the scoped EF Core DbContext for its lightweight transaction. Narrow query ports are permitted only where a feature requires a query EF Core cannot expose through that write convention.

## Persistence

PostgreSQL hosts one MDSweep application database and the existing separate Keycloak database. MDSweep continues to use checked-in EF Core migrations for application schema; Wolverine uses its own persistence schema for its conjoined-tenant registry and message persistence. Use EF Core mappings near the owning feature. Preserve broker-provided details separately from Tenant-owned changes and retain append-only history for Manifest receipts, Assignments, actual timestamps, outcomes, corrections, and closure.

New domain and application code uses NodaTime: `Instant` for timeline events, `LocalDate` for service dates, and `LocalTime` for local scheduled or appointment times. Time-zone conversion requires an explicit Tenant IANA time zone and never inherits the server time zone. New entity and idempotent-action identifiers use UUIDv7 through `Guid.CreateVersion7()` while remaining PostgreSQL `uuid` columns.

Domain factories enforce preconditions with Ardalis Guard Clauses, including repository `GuardClauseExtensions` where they express the invariant. Factories do not silently trim, normalize, or otherwise rewrite supplied values: invalid input is rejected and valid input is preserved as supplied.

Mutating Wolverine handlers opt into the lightweight EF Core transaction middleware explicitly. It calls one `SaveChangesAsync` at the end of a successful handler and relies on EF Core's transaction for that save. Read handlers do not open write transactions. Driver access creation remains an explicit-save command so a failed local commit can compensate by deleting the new Keycloak user. Assignment also saves explicitly so a uniqueness race can retain the established HTTP 409 conflict response. These two commands do not use Wolverine transaction middleware.

## Web application

Angular is organized as a small authenticated shell with lazy Dispatcher and Driver routes. TanStack Query owns server-state fetching, invalidation, and mutations. TanStack Table is limited to the dense Manifest review and daily Trips tables. Spartan primitives and Tailwind provide the UI foundation. The Driver offline action queue and its local Trip fallback remain explicit durable browser workflows; the general TanStack Query cache is not persisted.

The pilot may colocate PostgreSQL with the application on one small Linux host when the chosen BAA-covered environment and backup design permit it. Encrypted off-machine backups and a tested restore are required before real data is used.

## Suggested source layout

```text
src/
  Mdsweep.Api/
    Features/
      Trips/
      Access/
  Mdsweep.Application/
  Mdsweep.Domain/
  Mdsweep.Infrastructure/
  Mdsweep.AppHost/
  Mdsweep.ServiceDefaults/
  Mdsweep.Web/
tests/
  Mdsweep.Api.IntegrationTests/
  Mdsweep.Web.E2ETests/
```

HTTP endpoints remain in `Mdsweep.Api`, command/query contracts in `Mdsweep.Application`, domain state and rules in `Mdsweep.Domain`, and EF/file/Keycloak handlers and adapters in `Mdsweep.Infrastructure`. Organize each project vertically by behavior. Shared code must represent a stable cross-feature concept; proximity alone is not a reason to create a shared abstraction.

## Verification

Test through module interfaces and observable outcomes:

- Parser tests use synthetic manifests representing the known MTM column shape and edge cases.
- Workflow integration tests run against PostgreSQL and cover repeat imports, preserved overrides, Journey assignments, timestamp idempotency, corrections, and authorization.
- Angular tests cover focused interaction behavior.
- Playwright covers the dispatcher and driver workflows in this application, including the PWA's offline queue. It does not automate MTM Link.

The pilot acceptance path is: import a representative manifest without spreadsheet repair, apply bulk pickup suggestions, resolve exceptions, assign Journeys, complete Driver actions online and offline, retain every timestamp, and produce a validated billing file once its contract is known.

## Deferred decisions

- Unresolved MTM portal validation, duplicate, correction, signature-document, and partial-failure behavior
- Any authorized MTM API or browser automation
- Live mapping provider
- Managed PostgreSQL migration
- Arabic and other translations
- Automatic assignment and route optimization
