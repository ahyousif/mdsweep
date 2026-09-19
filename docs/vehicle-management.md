# Vehicle management

Open **Vehicles** in the sidebar to manage the selected Tenant's Vehicle records. Administrators and Dispatchers can use this page and its API; Drivers cannot.

## Workflow

- Search by display label, VIN, Year, Make, or Model, and filter by All, Active, or Inactive. Counts describe the complete Tenant list. The small reference list is fetched through a typed query and filtered locally, matching the Users page.
- Choose **Add vehicle**, enter a display label and VIN, and save. New Vehicles are active.
- Select a Vehicle to open its details. Choose **Edit** to change the label or VIN. An inactive Vehicle may be edited without reactivating it.
- Choose **Deactivate vehicle** or **Reactivate vehicle** to change its active state. These actions preserve the record and can be reversed. There is no delete operation.
- Search and filters remain after changes. Failed saves preserve the form draft and provide actionable feedback. Duplicate-VIN feedback directs the user to locate and edit or reactivate the existing record.

Year is optional and must be a whole number between 1900 and next year. Make and Model are optional and limited to 100 characters each. Omitting or clearing these fields on update clears their values. Search includes Year, Make, and Model. The existing `AddVehicles` migration includes these nullable columns for fresh databases.

Display labels are required and limited to 100 characters. VINs are required and must be exactly 17 ASCII letters or digits, excluding I, O, and Q. Input casing is preserved. Spaces and punctuation are rejected by API validation, with matching frontend feedback. The aggregate retains required-value guards without duplicating format rules. There is no VIN checksum or MTM registration check.

VIN uniqueness is case-sensitive and applies within the selected Tenant and includes inactive Vehicles. Other Tenants may independently record the same VIN. A database unique index prevents concurrent duplicate creation or editing. The handler's duplicate pre-check returns localized `vehicleVinExists` business feedback with English diagnostics. A concurrent conflict that passes the pre-check remains an unhandled database error; it does not receive the localized duplicate feedback.

## Implementation

Vehicles follows the Passengers module's endpoint/request/validator, command/query/handler, aggregate, specification, and persistence conventions. Commands and queries do not accept a Tenant ID. Existing authorization and conjoined tenancy supply the active Tenant.

Creation returns the new Vehicle model from its command handler without a follow-up query. The POST endpoint returns that model with `201 Created` and a Location header; GET remains a separate endpoint. Create and update endpoints map the handler's result directly without catching persistence exceptions.

| Method | Endpoint | Operation |
| --- | --- | --- |
| GET | `/api/vehicles` | List the Tenant's records |
| GET | `/api/vehicles/{id}` | Inspect one record |
| POST | `/api/vehicles` | Create using `displayLabel`, `vin`, and optional `year`, `make`, `model` |
| PUT | `/api/vehicles/{id}` | Edit `displayLabel`, `vin`, `year`, `make`, and `model` |
| PUT | `/api/vehicles/{id}/active` | Set explicit `isActive` |

Cross-Tenant IDs return 404. Drivers and inactive memberships are denied. Cookie-authenticated writes require the existing antiforgery token.

`AddVehicles` is an additive migration after `InitialSchema`. It creates only the Vehicles table and its Tenant/VIN unique index. The previous baseline remains intact. Tests verify a fresh database and upgrading the baseline with existing Tenant data.

## Design and localization

The [page concept](specs/designs/vehicles-page-v-1.png) uses the existing Users list, filters, details, and form patterns. The lower panels in the concept are alternative states. The implementation uses the existing application shell and shared UI tokens rather than reproducing unimplemented navigation items in the concept. Forms use the existing dialog primitives.

All text, validation, feedback, and accessible labels ship in English and Arabic. VINs remain left-to-right with western digits; display labels use directional isolation. Live switching preserves UI state.

On narrow screens, opening Vehicle details moves keyboard focus into the panel and makes the covered list inert. Tab stays within the panel; Escape or Close returns focus to the selected row, or to search if a status filter has removed that row. The edit dialog manages its own focus while open. Desktop details leave the list accessible.

## Scope and verification

This module manages only Vehicle records. Driver Primary Vehicles, assignments, Vehicle exceptions, Performed Vehicle snapshots, and billing remain future Trips integration. No existing Trips behavior is removed or changed. Year, Make, and Model are optional descriptive fields. License plate, insurance, maintenance, capacity, and location fields remain outside scope.

PostgreSQL HTTP tests cover lifecycle operations, validation, duplicate handling including concurrent requests, Tenant isolation, authorization, antiforgery, and schema upgrades. Playwright tests use synthetic API fixtures to verify management in both languages, draft preservation after duplicate rejection, filters, live language switching, mobile layouts, accessibility, load recovery, and Driver exclusion. Run the normal repository build/test checks and `npm run test:e2e -- vehicles.spec.ts --workers=1` in the web project.

Historical issue #5 describes a broader, earlier combined Driver/Vehicle assignment implementation. This standalone module fulfills the record-management portion under ADR 0008; it does not mark the remaining Trip integration or billing requirements complete.
