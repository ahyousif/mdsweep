# Replace manual MTM manifest processing

## Problem Statement

The Provider currently downloads an MTM Manifest, manually copies its columns into a separate spreadsheet and legacy operations site, estimates every Scheduled Pickup Time, manually assigns Drivers, and later opens Trips individually to prepare billing. The legacy site is not owned by this project and retains less than one week of history. This repeated data entry costs hours and creates opportunities for missed Trips, duplicate work, and incorrect billing data.

## Solution

Replace the legacy operations site with one deliberately small workflow. A Dispatcher uploads an MTM Manifest once; the application validates and imports Trips, groups related outbound and return Trips into Journeys, suggests Scheduled Pickup Times in bulk, and supports manual Driver assignment. Drivers use an installable web application to view only their assigned Trips and record Actual Pickup Time and Actual Drop-off Time. The application retains Operational History and exports a billing spreadsheet that the Provider manually uploads to MTM.

The MVP keeps human decisions where they matter and automates repetitive copying and calculation. MTM file exchange remains manual at both ends.

## User Stories

1. As a Dispatcher, I want to sign in securely, so that Provider operations are not publicly accessible.
2. As a Driver, I want an individual account, so that my actions are attributable to me.
3. As a Dispatcher, I want to manage Driver access through the Provider's identity service, so that only active Drivers can access assignments.
4. As a Dispatcher, I want to upload an MTM CSV or supported spreadsheet, so that I do not copy Trip fields manually.
5. As a Dispatcher, I want an import preview, so that I understand what will happen before records change.
6. As a Dispatcher, I want ready, warning, and blocked counts, so that I review exceptions instead of every normal row.
7. As a Dispatcher, I want invalid rows explained in plain language, so that I can correct them without editing the source file.
8. As a Dispatcher, I want valid rows imported even when other rows are blocked, so that one bad row does not stop the entire Manifest.
9. As a Dispatcher, I want broker-invalid or turned-back Trips retained but inactive, so that they remain in history without being assigned accidentally.
10. As a Dispatcher, I want repeated imports to avoid duplicate Trips, so that I can safely upload revised Manifests.
11. As a Dispatcher, I want broker changes distinguished from Operational Record changes, so that re-importing does not erase my work.
12. As a Dispatcher, I want outbound and return Trips grouped as a Journey, so that I can understand the passenger's complete visit.
13. As a Dispatcher, I want each Journey leg to remain independently editable, so that a return Trip can use a different Driver.
14. As a Dispatcher, I want Scheduled Pickup Times suggested in bulk, so that a large Manifest does not require one calculation per Trip.
15. As a Dispatcher, I want normal suggestions applied in one reversible action, so that automation saves time without hiding changes.
16. As a Dispatcher, I want uncertain suggestions placed in a Needs Review queue, so that I focus only on exceptions.
17. As a Dispatcher, I want to override a Scheduled Pickup Time, so that operational judgment remains authoritative.
18. As a Dispatcher, I want will-call Trips shown without an invented pickup time, so that the schedule remains truthful.
19. As a Dispatcher, I want a simple day board, so that I can see time, passenger, route, service type, Driver, and status without a wall of columns.
20. As a Dispatcher, I want filters for unassigned, needs-review, incomplete, and Driver, so that I can quickly find work requiring attention.
21. As a Dispatcher, I want to assign a Journey to one Driver by default, so that outbound and return legs do not require repetitive assignment.
22. As a Dispatcher, I want to reassign one Trip independently, so that real operational changes are supported.
23. As a Driver, I want to see only my assigned Trips, so that the application is simple and exposes only necessary passenger information.
24. As a Driver, I want call and navigation actions, so that I can perform the assigned Trip without re-entering details.
25. As a Driver, I want one-tap pickup and drop-off actions, so that actual timestamps require minimal effort.
26. As a Driver, I want offline actions visibly queued and later synchronized, so that weak connectivity does not lose timestamps.
27. As a Driver, I want to report a Trip that could not be completed using a standardized reason, so that the Dispatcher receives usable information.
28. As an authorized user, I want mistaken timestamps corrected with a reason, so that records are accurate without losing history.
29. As a Dispatcher, I want Driver actions, corrections, imports, and assignments retained, so that operational history lasts beyond one week.
30. As a Dispatcher, I want to distinguish Completed Trips from Closed Trips, so that incomplete billing information is not exported accidentally.
31. As a Dispatcher, I want Closed Trips validated for billing, so that missing required data is found before MTM upload.
32. As a Dispatcher, I want one MTM-compatible billing file, so that I do not open and enter every Trip individually.
33. As a Dispatcher, I want to download a daily operational spreadsheet when needed, so that operations have a simple fallback.
34. As the Provider, I want the normal workflow to remain usable in English initially, so that language support does not delay validation.
35. As the product team, we want to measure hands-on processing time before and after adoption, so that claimed savings are honest and Provider-specific.

## Implementation Decisions

- Use .NET 10, .NET Aspire, ASP.NET Core, Angular, EF Core, and PostgreSQL.
- Build a modular monolith using slim vertical slices for Manifest Import, Dispatch, Driver Work, Billing Export, and Identity.
- Use Keycloak as the OpenID Connect identity provider. The Angular PWA uses authorization code flow with PKCE; the API validates bearer tokens and authorizes the `Dispatcher` and `Driver` roles.
- Model one Provider in the MVP. Users belong to that Provider, but tenant resolution and tenant administration are deferred.
- Manifest Import owns parsing, normalization, validation, Trip identity, repeat-import comparison, broker-status handling, and Journey grouping.
- Broker-original facts, Provider overrides, and append-only Operational History remain distinct.
- Dispatch owns bulk Scheduled Pickup Time suggestions and manual assignments. It does not automatically select Drivers.
- The first travel-time implementation may use configured route or city presets plus arrival and loading buffers. Live mapping is deferred.
- Applying ordinary pickup suggestions is a reversible batch operation; exceptions remain visible for focused review.
- Assigning a Journey applies one Driver to its uncompleted Trips by default. Individual Trips remain reassignable.
- Driver Work is a PWA delivered through the browser without app-store distribution.
- Device capture time and server receipt time are separate facts. Offline actions are idempotent and expose Saved, Waiting to Sync, or Needs Attention.
- ASP.NET Core endpoints enforce that Drivers access only their assignments and Dispatchers access Provider-wide operations.
- EF Core accesses PostgreSQL directly inside the owning feature; there is no generic repository layer.
- MTM input and billing output remain user-initiated file workflows.
- Billing Export remains behaviorally blocked until the authoritative MTM bulk-upload template and training establish required fields, evidence, validation, duplicate handling, and rejection behavior.
- Wolverine PostgreSQL persistence/transport, durable queues, a separate automation worker, and MTM-specific Playwright automation are deferred until an authorized durable automation workflow exists. In-process Wolverine HTTP dispatch and EF Core unit-of-work handling do not change this deferral.
- Development, tests, issues, logs, and screenshots use synthetic patient data.
- Production hosting targets a small BAA-covered Linux deployment with encrypted off-machine backups and a tested restore, under an initial infrastructure target of $75 per month.
- English is the MVP language. Store controlled UI text and status codes in a localization-ready form so future Arabic support does not require rewriting domain state.
- The first client receives a supervised pilot. Product validation is whether the complete Manifest can be processed without column copying or per-Trip billing entry and with materially lower hands-on time.

## Testing Decisions

Good tests verify observable behavior through a confirmed module interface and survive internal refactoring. Tests do not mock internal feature collaborators, test private methods, or target a coverage percentage.

The primary seam is the ASP.NET Core HTTP workflow running against PostgreSQL. Integration tests cover Manifest upload and validation, repeat imports, preserved overrides, Journey grouping, bulk scheduling behavior, assignments, authorization, timestamp ordering and idempotency, correction history, and Billing Export once its contract is known.

The final smoke seam is one browser workflow after the end-to-end path exists. A small Playwright test verifies that a Dispatcher can import and assign a Trip and that the assigned Driver can record actual timestamps. Playwright is not used for every red-green cycle and does not automate MTM Link.

Angular behavior may receive focused tests where interaction logic is substantial. Real pilot bugs receive regression tests at the highest stable seam.

The repository has no existing application tests. Synthetic Manifest fixtures and worked scheduling examples provide independent expected values.

## Out of Scope

- MTM API integration
- MTM portal browser automation
- Automatic Driver assignment
- Route optimization
- App Store or Play Store distribution
- Live GPS tracking unless later shown to be required for the billing file
- Signatures or extra Driver events unless the MTM bulk-upload contract requires them
- Arabic or additional UI translations
- Other brokers
- Payment reconciliation
- Payroll, fleet maintenance, credential management, or general NEMT management
- Multiple Providers and tenant administration
- Managed PostgreSQL migration
- Broad Playwright coverage, visual snapshots, and coverage-percentage targets

## Further Notes

Two inputs remain intentionally pinned rather than guessed:

1. Observe 5–10 representative scheduling decisions to derive the initial pickup-time policy and worked test cases.
2. Review MTM's bulk-upload training and template before specifying or implementing Billing Export.

The first usable tracer bullet is: upload a synthetic MTM Manifest, preview its validation summary, apply the import, and display the imported Trips without spreadsheet repair.
