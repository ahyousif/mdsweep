# Trips and Access replacement plan

## Outcome

Replace the unused stage-based backend with deep `Passengers` and `Trips` modules plus a separate `Access` module. Preserve validated behavior and security constraints, but do not preserve old namespaces, HTTP contracts, or the synthetic database shape for compatibility. Angular and Playwright are intentionally unchanged in this backend phase.

The completed workflow is:

```text
Manifest receipt
    → Passenger and Trip reconciliation
    → pickup planning
    → Driver Assignment
    → actual pickup/drop-off and Trip Outcome
    → Dispatcher review and closure
    → Billing Batch and MTM workbook
```

Manual MTM file exchange remains at both ends. Vehicle management and Vehicle Assignment remain out of scope.

## Module ownership

### Passengers

Passengers owns Passenger identity, Provider-owned contact details and notes, and broker-specific member identifiers. A Passenger may be created independently of a Manifest or Trip. Trips references a Passenger but does not own Passenger identity.

### Trips

Trips owns Trip identity, Manifest receipts, Journey relationships, pickup planning, Assignment, actual timestamps, Trip Outcomes, Timestamp Corrections, closure, Billing Readiness, Billing Batches, and Operational History.

Organize its implementation internally by behavior:

```text
Passengers/
  Management/
Trips/
  Intake/
  Planning/
  Assignments/
  Performance/
  Review/
  Billing/
```

These folders do not present independent module interfaces. HTTP endpoints, CSV/XLSX readers, offline-action ingestion, and the billing writer are adapters over Trips rather than owners of its state.

### Access

Access owns Users, Provider Memberships, role authorization, and Keycloak mappings. Driver and Dispatcher are membership roles. A Driver Profile is linked to a User and contains the operational identifier required for Assignment; Dispatcher has no separate profile.

## Technical foundations

- Use NodaTime throughout new domain, application, persistence, and HTTP contracts.
  - `Instant` represents an event on the global timeline, including receipt, assignment, correction, closure, and export times.
  - `LocalDate` represents a service or appointment date.
  - `LocalTime` represents a scheduled or appointment wall-clock time.
  - A Driver action retains both its device-captured `Instant` and server-received `Instant`.
  - Time-zone conversion occurs only at an explicit interface using the Provider's configured IANA time zone; do not infer it from the server.
- Generate new entity and action identifiers with `Guid.CreateVersion7()`.
- Continue storing UUIDs in PostgreSQL `uuid` columns; UUIDv7 changes generation and ordering characteristics, not the database type.
- Configure the Npgsql NodaTime plugin and System.Text.Json NodaTime converters at their respective seams.
- Keep EF Core inside each owning implementation; do not add repository interfaces.
- Keep the checked-in EF migration chain and add a schema-replacement migration. No production-data backfill is required.

## Public interfaces and test seams

The primary interface and acceptance-test seam is the authenticated HTTP workflow backed by PostgreSQL. The interface is organized around behaviors rather than the removed modules:

- review and accept a Manifest;
- create, find, update, and inspect a Passenger's Trip history;
- view daily Trips and apply pickup plans;
- assign a Journey or Trip to a Driver;
- view the authenticated Driver's assigned Trips;
- record actual pickup/drop-off and terminal outcomes online or offline;
- correct timestamps while preserving the original;
- review and close terminal Trips;
- prepare and download a Billing Batch.

Frontend interaction and Playwright seams are deferred until the backend replacement is accepted. Tests use synthetic data only.

## Replacement slices

Each slice follows one failing public test, the minimum implementation that passes, and then the next behavior. Old code is deleted as soon as its replacement passes; no compatibility layer is retained.

1. **Foundation**
   - Add NodaTime persistence and JSON support.
   - Introduce UUIDv7 generation.
   - Establish `Trips` and `Access` namespaces and shared actor context.
2. **Passenger management**
   - Create and find Passengers independently of Manifests.
   - Maintain Provider-owned contact information and notes.
   - View Passenger Trip history.
3. **Manifest receipt**
   - Parse CSV/XLSX through adapters.
   - Review every row and reconcile Passengers, Trips, and Journeys idempotently.
   - Preserve broker-provided details separately from Provider-owned changes.
4. **Planning and Assignment**
   - Calculate and apply Scheduled Pickup Times, including will-call behavior.
   - Assign Journeys or individual Trips to active Driver Profiles.
   - Retain superseded Assignments and exclude Vehicles.
5. **Trip performance**
   - Limit Drivers to their active Assignments.
   - Record actual pickup/drop-off and terminal outcomes.
   - Retain device-captured and server-received Instants.
   - Make online and offline actions idempotent and retain unsafe conflicts for review.
6. **Correction and closure**
   - Correct timestamps with a reason while preserving originals.
   - Leave correction authorization and time limits configurable until product policy is resolved.
   - Close reviewed terminal Trips independently of Billing Readiness.
7. **Billing**
   - Validate Closed Trips against the accepted MTM contract.
   - Retain immutable Billing Batch membership and generate the workbook for manual upload.
8. **Removal and verification**
   - Delete `ManifestImports`, `Dispatch`, `DriverWork`, `Identity`, Vehicle code, and obsolete contracts/tests.
   - Run formatting and the complete .NET build and PostgreSQL-backed integration tests.

## Completion constraints

- Authorization is verified at the HTTP seam and inside resource-sensitive behavior.
- Broker-provided details, Provider-owned changes, and append-only Operational History remain distinct.
- Repeat Manifests and offline Driver actions are idempotent.
- Failures are actionable to the User.
- All development and verification data remains synthetic until deployment readiness is accepted.
