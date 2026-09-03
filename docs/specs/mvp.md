# Replace manual MTM manifest processing

## Problem Statement

The Provider currently downloads an MTM Manifest, manually copies its columns into a separate spreadsheet and legacy operations site, estimates every Scheduled Pickup Time, manually assigns Drivers, and later opens Trips individually to prepare billing. The legacy site is not owned by this project and retains less than one week of history. This repeated data entry costs hours and creates opportunities for missed Trips, duplicate work, and incorrect billing data.

## Solution

Replace the legacy operations site with one deliberately small workflow. A Dispatcher uploads an MTM Manifest once; the application validates and imports Passengers and Trips, groups related outbound and return Trips into Journeys, suggests Scheduled Pickup Times in bulk, and supports manual Driver assignment. Dispatchers can also create and maintain Passengers independently of a Manifest. Drivers use an installable web application to view only their assigned Trips and record Actual Pickup Time and Actual Drop-off Time. The application retains Operational History and exports a billing spreadsheet that the Provider manually uploads to MTM.

The MVP keeps human decisions where they matter and automates repetitive copying and calculation. MTM file exchange remains manual at both ends.

## User Stories

1. As a Dispatcher, I want to sign in securely, so that Provider operations are not publicly accessible.
2. As a Driver, I want an individual account, so that my actions are attributable to me.
3. As a Dispatcher, I want to manage Driver access through the Provider's identity service, so that only active Drivers can access assignments.
4. As a Dispatcher, I want to find or create a Passenger independently of a Manifest, so that passenger management is not limited to imported Trips.
5. As a Dispatcher, I want to maintain Provider-owned Passenger contact information and notes, so that operations can use current information without erasing what the broker supplied.
6. As a Dispatcher, I want to view a Passenger's Trip history, so that I can understand and support their transportation history.
7. As a Dispatcher, I want to upload an MTM CSV or supported spreadsheet, so that I do not copy Trip fields manually.
8. As a Dispatcher, I want an import preview, so that I understand what will happen before records change.
9. As a Dispatcher, I want ready, warning, and blocked counts, so that I review exceptions instead of every normal row.
10. As a Dispatcher, I want invalid rows explained in plain language, so that I can correct them without editing the source file.
11. As a Dispatcher, I want valid rows imported even when other rows are blocked, so that one bad row does not stop the entire Manifest.
12. As a Dispatcher, I want broker-invalid or turned-back Trips retained but inactive, so that they remain in history without being assigned accidentally.
13. As a Dispatcher, I want repeated imports to avoid duplicate Trips, so that I can safely upload revised Manifests.
14. As a Dispatcher, I want broker-provided changes distinguished from Provider-owned changes, so that re-importing does not erase my work.
15. As a Dispatcher, I want outbound and return Trips grouped as a Journey, so that I can understand the Passenger's complete visit.
16. As a Dispatcher, I want each Journey leg to remain independently editable, so that a return Trip can use a different Driver.
17. As a Dispatcher, I want Scheduled Pickup Times suggested in bulk, so that a large Manifest does not require one calculation per Trip.
18. As a Dispatcher, I want normal suggestions applied in one reversible action, so that automation saves time without hiding changes.
19. As a Dispatcher, I want uncertain suggestions placed in a Needs Review queue, so that I focus only on exceptions.
20. As a Dispatcher, I want to override a Scheduled Pickup Time, so that operational judgment remains authoritative.
21. As a Dispatcher, I want will-call Trips shown without an invented pickup time, so that the schedule remains truthful.
22. As a Dispatcher, I want a simple day board, so that I can see time, Passenger, route, service type, Driver, and outcome without a wall of columns.
23. As a Dispatcher, I want filters for unassigned, needs-review, incomplete, and Driver, so that I can quickly find work requiring attention.
24. As a Dispatcher, I want to assign a Journey to one Driver by default, so that outbound and return legs do not require repetitive Assignment.
25. As a Dispatcher, I want to reassign one Trip independently, so that real operational changes are supported.
26. As a Driver, I want to see only my assigned Trips, so that the application is simple and exposes only necessary Passenger information.
27. As a Driver, I want call and navigation actions, so that I can perform the assigned Trip without re-entering details.
28. As a Driver, I want one-tap pickup and drop-off actions, so that actual timestamps require minimal effort.
29. As a Driver, I want offline actions visibly queued and later synchronized, so that weak connectivity does not lose timestamps.
30. As a Driver, I want to report a Trip that could not be completed using a standardized reason, so that the Dispatcher receives usable information.
31. As an authorized User, I want mistaken timestamps corrected with a reason, so that records are accurate without losing history.
32. As a Dispatcher, I want Driver actions, corrections, Manifest receipts, and Assignments retained, so that Operational History lasts beyond one week.
33. As a Dispatcher, I want terminal Trips reviewed and explicitly closed, so that unresolved operational information is not exported accidentally.
34. As a Dispatcher, I want Closed Trips validated for billing, so that missing required data is found before MTM upload.
35. As a Dispatcher, I want to maintain the Tenant's list of Vehicles registered in MTM, so that billing uses known VINs without retyping them for every Trip.
36. As a Dispatcher, I want to designate a Driver's Primary Vehicle before assigning Trips, so that their usual Vehicle can be reused across Trips on a service date.
37. As a Dispatcher, I want each billable Trip to use the Driver's Primary Vehicle automatically unless I record an exception, so that billing reflects performed work without repetitive confirmation.
38. As a Dispatcher, I want one MTM-compatible billing file, so that I do not open and enter every Trip individually.
39. As a Dispatcher, I want to download a daily operational spreadsheet when needed, so that operations have a simple fallback.
40. As the Provider, I want the normal workflow to remain usable in English initially, so that language support does not delay validation.
41. As the product team, we want to measure hands-on processing time before and after adoption, so that claimed savings are honest and Provider-specific.

## Implementation Decisions

- Use .NET 10, .NET Aspire, ASP.NET Core, Angular, EF Core, and PostgreSQL.
- Build a modular monolith with deep Passengers and Trips modules plus a separate Access module. Passenger management is independent of Trip lifecycle; organize Manifest intake, planning, performance, review, and billing as internal vertical slices of Trips rather than top-level domain modules.
- Use Keycloak as the OpenID Connect identity provider. ASP.NET Core is the confidential OpenID Connect client and backend-for-frontend: it completes the authorization-code flow, establishes an HttpOnly application cookie, resolves the active Provider context, and authorizes the `Dispatcher` and `Driver` roles. The Angular PWA calls same-origin application endpoints and never receives or stores Keycloak access or refresh tokens.
- Model one Provider in the MVP. Users belong to that Provider, but tenant resolution and tenant administration are deferred.
- Passengers owns Passenger identity and Provider-owned Passenger information. Trips owns Trip identity, repeat-Manifest comparison, Journey grouping, planning, Assignment, actual timestamps, outcomes, corrections, closure, billing readiness, and Operational History.
- Passenger is a durable Provider-owned entity that can be created, found, and maintained independently of a Trip. Preserve broker-provided Passenger details separately from Provider-owned changes.
- Broker-original facts, Provider overrides, and append-only Operational History remain distinct.
- Trips owns bulk Scheduled Pickup Time suggestions and manual Assignments. It does not automatically select Drivers.
- The first travel-time implementation may use configured route or city presets plus arrival and loading buffers. Live mapping is deferred.
- Applying ordinary pickup suggestions is a reversible batch operation; exceptions remain visible for focused review.
- Assigning a Journey applies one Driver to its uncompleted Trips by default. Individual Trips remain reassignable.
- The Driver experience is a PWA delivered through the browser without app-store distribution.
- Device capture time and server receipt time are separate facts. Offline actions are idempotent and expose Saved, Waiting to Sync, or Needs Attention.
- Timestamp corrections preserve the original time and require a reason. Who may correct a timestamp and for how long remain deliberately unresolved.
- A Trip may be closed after its terminal outcome and required operational information have been reviewed and accepted. Closure and billing readiness remain separate decisions.
- Users receive Driver and Dispatcher roles through Provider Membership; a User may hold both roles. A Driver Profile contains the operational information needed for Assignment, while Dispatcher remains a role.
- Maintain a minimal Tenant-owned reference list of Vehicles that a Dispatcher confirms are registered in MTM. Each Vehicle has a display label, VIN, and active state; registering the Vehicle with MTM remains an external process.
- A Dispatcher may designate one active Vehicle as a Driver Profile's Primary Vehicle before the Driver has Trip Assignments. Primary Vehicle changes are effective-dated and retained so the correct default can be resolved for each Trip's service date.
- A Trip uses the assigned Driver's Primary Vehicle for its service date as the Performed Vehicle unless a Dispatcher records an exception. Ordinary Trips require no per-Trip Vehicle confirmation.
- The Dispatcher can override the Vehicle for one Trip or apply an exception across selected Trips. A missing Driver, missing Primary Vehicle, or inactive Vehicle places the Trip in Needs Review instead of guessing.
- When a Trip is closed, preserve the resolved Performed Vehicle VIN as historical Trip data so later Vehicle edits, deactivation, or Primary Vehicle changes cannot rewrite prior work or claims. Drivers do not select Vehicles.
- MDSweep records the Tenant's confirmation but does not independently verify current MTM registration or Driver/Vehicle eligibility. MTM Link remains authoritative during manual upload.
- ASP.NET Core endpoints enforce that Drivers access only their assignments and Dispatchers access Provider-wide operations.
- EF Core accesses PostgreSQL directly inside the owning feature; there is no generic repository layer.
- MTM input and billing output remain user-initiated file workflows.
- Billing Export uses the supplied MTM bulk-upload template, but production compatibility remains gated on the bounded synthetic portal trial. Client confirmation is still required for which Trip outcomes require a VIN and whether outbound and return Trips may use different Vehicles.
- MDSweep does not store or manage signature documents in the MVP. The Dispatcher continues uploading the generic signature document accepted by the current MTM Link workflow, and exported claim rows use the accepted signed-log indication.
- Wolverine PostgreSQL persistence/transport, durable queues, a separate automation worker, and MTM-specific Playwright automation are deferred until an authorized durable automation workflow exists. In-process Wolverine HTTP dispatch and EF Core unit-of-work handling do not change this deferral.
- Development, tests, issues, logs, and screenshots use synthetic Passenger data.
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
- Signature-document storage or management; the existing generic signature remains a manual MTM Link upload
- Arabic or additional UI translations
- Other brokers
- Payment reconciliation
- Payroll, fleet maintenance, credential management, or general NEMT management
- Per-Trip vehicle optimization; registering Vehicles with MTM from MDSweep; and broader Vehicle management such as maintenance, inspections, insurance, credentials, capacity planning, and location tracking
- Multiple Providers and tenant administration
- Managed PostgreSQL migration
- Broad Playwright coverage, visual snapshots, and coverage-percentage targets

## Further Notes

Two inputs remain intentionally pinned rather than guessed:

1. Observe 5–10 representative scheduling decisions to derive the initial pickup-time policy and worked test cases.
2. Confirm the remaining billing questions with the client and through a bounded synthetic MTM Link trial before implementing production Billing Export validation.

The first usable tracer bullet is: upload a synthetic MTM Manifest, preview its validation summary, accept it, and display the resulting Passengers and Trips without spreadsheet repair.
