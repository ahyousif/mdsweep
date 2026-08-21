# NEMT Operations

This context describes the daily work of turning broker-provided transportation requests into assigned, completed, and billable non-emergency medical transportation trips.

## Language

**Provider**:
The transportation business responsible for scheduling, performing, and billing trips.
_Avoid_: Client, account

**App User**:
The MDSweep-owned identity record for a person. It maps to an external identity-provider subject but remains the local reference used by operational records.

**Provider Membership**:
An App User's authorized relationship to a Provider, including coarse operational roles such as Dispatcher or Driver.

**Dispatcher**:
The provider user who imports requested trips, plans pickup times, assigns drivers, and resolves operational exceptions.
_Avoid_: Admin

**Driver**:
The provider user who views assigned trips and records when passengers are actually picked up and dropped off.

**Trip**:
A broker-authorized passenger movement from one pickup location to one drop-off location at an expected time.
_Avoid_: Ride, job

**Journey**:
A passenger's related outbound and return trips for the same visit. Each trip remains independently assignable and completable.
_Avoid_: Round Trip

**Manifest**:
A broker export containing trips offered or assigned to the provider for a service period.
_Avoid_: Spreadsheet, upload

**Scheduled Pickup Time**:
The dispatcher's planned time for a driver to pick up the passenger, based on appointment time and operational judgment.
_Avoid_: Pickup Time

**Will-call Trip**:
A return trip whose pickup time is not scheduled until the passenger reports being ready.

**Actual Pickup Time**:
The time the driver records that the passenger was picked up.

**Actual Drop-off Time**:
The time the driver records that the passenger was dropped off.

**Trip Outcome**:
The result recorded by the driver for an attempted trip, such as completed, passenger no-show, or cancelled.
_Avoid_: Trip Status

**Completed Trip**:
A trip for which the driver has recorded the physical transportation outcome and its required actual times.

**Closed Trip**:
A completed trip whose required operational and billing information has been reviewed and accepted by the dispatcher.

**Operational Record**:
The provider-owned version of a trip, including scheduling, assignment, and completion changes made after manifest import.

**Assignment**:
The dispatcher's selection of the driver responsible for a trip. Only one assignment is active at a time, while previous assignments remain part of the trip's history.

**Operational History**:
The retained record of trip imports, scheduling, assignments, outcomes, timestamps, corrections, and closure activity.
_Avoid_: Audit Log
