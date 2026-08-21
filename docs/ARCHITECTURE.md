# MVP Architecture

## Shape

Build a modular monolith with a slim vertical-slice structure. The deployed product has an Angular PWA, one ASP.NET Core application, and PostgreSQL. .NET Aspire composes the development environment and deployable resources.

The application replaces the provider's legacy operations site. MTM integration at both ends is file-based for the MVP:

```text
MTM manifest → import → dispatch → driver completion → billing export → MTM portal
```

The user downloads the manifest and uploads the billing file manually. Browser automation, Wolverine messaging, and an automation worker enter the architecture only when an authorized durable automation workflow exists.

## Modules

Each module owns a workflow and presents a small interface through its HTTP endpoints and application commands. Business rules, persistence, validation, and audit recording remain local to the slice that owns the behavior.

### Manifest Import

**Interface:** upload a manifest, inspect its validation summary, and apply the accepted import.

The implementation hides CSV/XLSX parsing, normalization, duplicate detection, A/B Journey grouping, broker-status handling, repeat-import comparison, and preservation of operational overrides. Applying the same source data repeatedly must not duplicate Trips or erase provider-owned changes.

### Dispatch

**Interface:** view a service day, apply pickup-time suggestions, and assign a Driver to a Journey or Trip.

The implementation hides bulk suggestion policy, exception classification, Journey-wide assignment, individual-leg reassignment, conflict warnings, and assignment history. Driver assignment remains a human decision.

### Driver Work

**Interface:** load the authenticated Driver's assigned Trips and record a Trip Outcome or actual timestamp.

The implementation hides authorization, valid event order, offline idempotency, correction windows, conflict detection, and audit history. Device capture time and server receipt time remain distinct facts.

### Billing Export

**Interface:** validate Closed Trips and generate one MTM-compatible billing file.

The implementation generates the exact ten-column `.xlsx` Claims Sheet documented in `docs/research/mtm-bulk-claim-upload.md`, validates claim readiness before export, and records the export and included Trips in Operational History. The Dispatcher continues the manual MTM Link review and submission workflow.

The file shape is established, but production compatibility remains gated on a bounded synthetic portal trial for the unresolved validation, duplicate, correction, signature-document, and partial-failure behavior recorded in the research note.

### Identity

ASP.NET Core Identity owns authentication through secure cookies. MVP roles are `Dispatcher` and `Driver`. Dispatchers access provider-wide operations; Drivers access only their own assigned Trips.

Identity supports the workflow modules and does not become a generic permission framework.

## Seams and adapters

### Travel-time estimation

Dispatch owns a narrow travel-time interface: estimate travel duration for a pickup, destination, and relevant departure context. Its first production adapter uses configured route or city presets. A future mapping adapter may replace it after contractual and privacy review. Tests use a deterministic adapter.

The interface returns an estimate or an actionable failure. It does not assign Drivers or decide the Scheduled Pickup Time; Dispatch combines the estimate with configured arrival and loading buffers.

### Time

Timestamp-sensitive Driver Work behavior receives time from an injected clock. Production uses system time and tests use a controlled clock so correction windows and event ordering are deterministic.

Avoid seams for EF Core persistence. Each slice uses the application's DbContext directly; test observable workflow behavior against PostgreSQL rather than wrapping it in a generic repository.

## Persistence

PostgreSQL is the single durable store. Use EF Core mappings near the owning feature. Preserve broker-original data separately from operational overrides and retain append-only history for imports, assignments, driver events, corrections, and closure.

The pilot may colocate PostgreSQL with the application on one small Linux host when the chosen BAA-covered environment and backup design permit it. Encrypted off-machine backups and a tested restore are required before real data is used.

## Suggested source layout

```text
src/
  AppHost/
  Api/
    Features/
      ManifestImports/
      Dispatch/
      DriverWork/
      BillingExports/
      Identity/
    Infrastructure/
  Web/
tests/
  Api.IntegrationTests/
  Web.E2ETests/
```

Organize endpoints, commands, validation, mappings, and tests by behavior inside each feature. Shared code must represent a stable cross-feature concept; proximity alone is not a reason to create a shared abstraction.

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
- Multitenancy
- Arabic and other translations
- Automatic assignment and route optimization
