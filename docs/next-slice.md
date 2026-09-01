# Backend slice handoff

## Current foundation

- Tenant, User, and TenantMembership provide the identity and authorization model.
- Passenger is the Tenant-owned reference vertical slice.
- TripImport previews and applies broker files idempotently.
- Trips support a Dispatcher-authorized GET and Scheduled Pickup Time mutation.
- Tenant-isolation integration coverage protects Passenger, Trip, and TripImport behavior.
- Broker facts remain separate from Tenant-owned operational state.

## Next slice: Dispatcher Service Day

Build a read-only daily Trip list for a Dispatcher opening one service day:

```text
Dispatcher opens a service day
→ sees the Tenant's Trips for that date
→ sees the information needed to begin planning
```

A likely API shape is `GET /api/service-days/{date}/trips`, subject to the established repository conventions for the slice.

The initial projection should use only existing facts: Trip ID, broker Trip number, Passenger display identity, service date, appointment time where represented, Scheduled Pickup Time, and pickup/drop-off information from broker facts.

Do not add Driver, Vehicle, Assignment, Journey, route calculation, actual pickup/drop-off, billing, or new scheduling rules. This projection-heavy query may be a future Dapper candidate, but that implementation decision belongs to the Service Day slice.
