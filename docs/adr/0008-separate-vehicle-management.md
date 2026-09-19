# Separate Vehicle record management from Trips

Vehicles is a separate module in the modular monolith. A Vehicle can be created, edited, deactivated, and reactivated without any Driver or Trip Assignment. Vehicles owns the Tenant-scoped reference record, display label, VIN validation and uniqueness, and active state. It has its own navigation item and page.

This supersedes only the earlier placement of Vehicle reference-record ownership inside Trips. Trips retains its existing behavior and all planned responsibilities for Driver Primary Vehicle designations, assignment, Vehicle exceptions, Performed Vehicle resolution, historical VIN snapshots, and billing. None of that integration is part of this management slice.

The module follows the existing Passengers patterns: feature endpoints and FluentValidation requests, Application commands/queries and handlers, a Domain aggregate, and EF Core configuration through the common IRepository. It shares the existing application and database; no service, generic fleet abstraction, or new project is introduced.

Administrators and Dispatchers manage Vehicles in their selected Tenant; Drivers cannot. VINs are required, preserved as entered, and validated at the API boundary to contain exactly 17 ASCII letters/digits excluding I, O, and Q. A database constraint enforces case-sensitive uniqueness within each Tenant, including inactive Vehicles. Deactivation preserves the record. Checksum validation and MTM registration/eligibility verification are outside scope.

The dedicated page follows the existing Users design and ships in English and Arabic. Broader fleet management, permanent deletion, and additional vehicle attributes remain excluded.
