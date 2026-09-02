# NEMT Operations

This context describes the daily work of turning broker-provided transportation requests into assigned, completed, and billable non-emergency medical transportation trips.

## Language

**Tenant**:
The transportation business whose Users, Passengers, Trips, and operational records are isolated from other businesses.
Each Tenant has a lowercase, unambiguous identifier in `xxxx-xxxx-xxxx` form.
_Avoid_: Provider, client, account

**User**:
A person who can authenticate to MDSweep. A User may hold more than one role for a Tenant.
_Avoid_: App User

**Tenant Membership**:
A User's authorized relationship to a Tenant, including roles such as Dispatcher or Driver.

**Dispatcher**:
A Tenant Membership role allowed to manage Passengers and Drivers, accept Manifests, plan and assign Trips, review outcomes, and prepare billing.
_Avoid_: Admin

**Driver**:
A Tenant Membership role allowed to view assigned Trips and record when their passengers are picked up and dropped off.

**Driver Profile**:
The Tenant-owned operational record that makes a User eligible for Assignment and retains the broker identifier used for that Driver.

**Vehicle**:
The Tenant's means of transportation registered with MTM and identified by its VIN. MTM remains authoritative for the Vehicle's registration.
_Avoid_: Car

**Primary Vehicle**:
The Vehicle a Dispatcher designates as a Driver's usual Vehicle for an effective period. It may be designated before the Driver has any Trip Assignments and supplies the default for their Trips during that period.

**Performed Vehicle**:
The Vehicle attributed to a performed Trip for billing, normally derived from the Driver's Primary Vehicle unless a Dispatcher records an exception. Its VIN is retained as historical Trip information.

**Passenger**:
The person for whom transportation is arranged and performed. A Passenger belongs to one Tenant and may be created independently of a Trip. A broker-specific member identifier distinguishes the Passenger within broker records. For the initial MTM template, the `Medicaid Number` identifies that Passenger.
_Avoid_: Patient, Client, Member

**Trip**:
A broker-authorized passenger movement from one pickup location to one drop-off location at an expected time.
_Avoid_: Ride, job

**Journey**:
A passenger's related outbound and return trips for the same visit. Each trip remains independently assignable and completable.
_Avoid_: Round Trip

**Manifest**:
A broker export containing trips offered or assigned to the Tenant for a service period.
_Avoid_: Spreadsheet, upload

**Manifest Receipt**:
The retained record of a Manifest received and reviewed by the Tenant, including the disposition of every source row.

**Trip Import**:
The retained preview of one uploaded broker file, including parsed source rows, validation outcomes, and whether it has been applied.
_Avoid_: Manifest Import, upload

**Broker Trip Facts**:
Facts supplied by the broker for a Trip, retained separately from Tenant operational decisions so a later import cannot overwrite those decisions.

**Scheduled Pickup Time**:
The dispatcher's planned time for a driver to pick up the passenger, based on appointment time and operational judgment.
_Avoid_: Pickup Time

**Will-call Trip**:
A return trip whose pickup time is not scheduled until the passenger reports being ready.

**Actual Pickup Time**:
The time the driver records that the passenger was picked up.

**Actual Drop-off Time**:
The time the driver records that the passenger was dropped off.

**Timestamp Correction**:
A reasoned correction to an Actual Pickup Time or Actual Drop-off Time that preserves the originally recorded time.

**Trip Outcome**:
The result recorded by the driver for an attempted trip, such as completed, passenger no-show, or cancelled.
_Avoid_: Trip Status

**Completed Trip**:
A trip for which the driver has recorded the physical transportation outcome and its required actual times.

**Closed Trip**:
A Trip whose terminal outcome and required operational information have been reviewed and accepted by the Dispatcher. Closure does not by itself establish billing eligibility.

**Assignment**:
The dispatcher's selection of the Driver responsible for a Trip. Only one Assignment is active at a time, while previous Assignments remain part of the Trip's history; the Performed Vehicle is recorded separately.

**Billing Readiness**:
The result of validating whether a Closed Trip contains the information required for billing.

**Billing Batch**:
The retained set of billing-ready Trips included in one generated billing file.

**Operational History**:
The retained record of trip imports, scheduling, assignments, outcomes, timestamps, corrections, and closure activity.
_Avoid_: Audit Log
