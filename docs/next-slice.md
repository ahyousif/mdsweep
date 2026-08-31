# Backend slice handoff

## Current foundation

- Tenant, User, and TenantMembership replace the legacy Provider identity model.
- Wolverine establishes Tenant context from the authenticated tenant claim and applies conjoined tenancy.
- Passengers is the reference vertical slice: thin API endpoint, Application handler, Domain aggregate, and common `IRepository` write convention.
- TripImports is complete enough to preview CSV/XLSX input and apply it idempotently into Passengers and Trips. Parsed import state is retained; import parsing is Infrastructure-only; orchestration is Application-only.
- A Trip keeps broker-owned facts separate from operational state. Broker re-imports must not overwrite `ScheduledPickupTime` or move an existing Trip to another Passenger.

## Next slice: Trip planning

Build the first clean behavior inside `Trips`: a Dispatcher can set and retrieve a Trip's Scheduled Pickup Time.

Scope:

1. Add a Trip query/read model and a Dispatcher-authorized `GET /api/trips/{id}` endpoint as needed by the planning workflow.
2. Add a `SetScheduledPickupTime` Application command and handler using `IRepository` and the existing Wolverine EF transaction convention.
3. Keep the rule on `TripAggregate`; use `LocalTime` for the scheduled pickup time and preserve broker facts unchanged.
4. Add a Tenant-scoped API test proving a Dispatcher can set the time and a different Tenant cannot read or modify the Trip.
5. Do not introduce Driver, Assignment, Vehicle, Journey, or route-calculation behavior in this slice.

## Deliberately deferred

- Passenger contact details and notes
- Driver Profile and assignment
- actual pickup/drop-off and corrections
- offline driver actions
- billing export

## Completion check

The slice is complete when its API workflow is authorized, tenant-isolated, PostgreSQL-backed, and covered by synthetic-data integration tests.
