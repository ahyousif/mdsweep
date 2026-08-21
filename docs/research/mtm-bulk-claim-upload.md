# MTM Link bulk claim upload contract

## Decision summary

The supplied first-party artifacts establish enough of the MTM Link contract to design a manual Billing Export: generate an `.xlsx` workbook with one claim row per Trip and the exact ten-column layout below, then let the Dispatcher upload it to MTM Link and finish the portal's review and submission flow.

They do **not** establish every validation detail needed for a production-safe exporter. Before claiming compatibility, run a synthetic portal trial for the unresolved items listed below. The application should continue to own operations and produce the workbook; it should not automate MTM Link.

## Sources and precedence

- **Workbook:** `MTM LInk Bulk Upload Template.xlsx`, worksheet `Sheet1`. The workbook was last modified on 2024-10-23 according to its package metadata.
- **Guide:** `MTM Link - Claiming a Trip Bulk Upload.pdf`, 12 pages, created 2024-08-08 according to its PDF metadata.

The workbook is newer and is the exact file-shaped artifact, so use its cells and formats as the source of truth when its presentation differs from a screenshot in the guide. Live MTM Link behavior remains authoritative for portal validation.

The supplied artifacts contain example operational identifiers. This note intentionally records only the contract, never the example values.

## Workbook structure

The file is an OOXML `.xlsx` workbook with exactly one visible worksheet named `Sheet1`. Its populated sample range is `Sheet1!A1:J11`: row 1 contains headers and rows 2–11 contain examples. It has no Excel Table, formulas, named ranges, hidden sheets or rows, worksheet protection, or embedded data-validation rules. Consequently, validation appears to be performed by MTM Link after upload rather than by Excel.

For export, preserve the worksheet name, header spelling, capitalization, and order until a synthetic portal test proves that any of those are flexible.

| Order | Header and source | Meaning | Observed workbook representation | Required/allowed values established by the artifacts |
|---:|---|---|---|---|
| 1 | `Trip Number` (`Sheet1!A1`) | MTM Trip Number | Text in `A2:A11` | The guide instructs the provider to enter it. No length or character rules are stated. (PDF p. 4) |
| 2 | `Driver` (`Sheet1!B1`) | MTM Driver Number; the review UI also refers to correcting a license number | Numeric, General format in `B2:B11` | The selected Driver must be approved and contractually compliant. No length or leading-zero behavior is stated. (PDF pp. 4, 7, 10) |
| 3 | `Vehicle` (`Sheet1!C1`) | Vehicle VIN Number | Text, General format in `C2:C11` | The selected Vehicle must be approved and contractually compliant. No VIN validation details are stated. (PDF pp. 4, 7, 10) |
| 4 | `ScheduledPickupTime` (`Sheet1!D1`) | Scheduled pickup | Excel time value in `D2:D11`, displayed with `[$-409]h:mm AM/PM;@` | Included among the required pickup/drop-off times. (PDF p. 4) |
| 5 | `ReportedPickupArriveTime` (`Sheet1!E1`) | Reported arrival at pickup | Excel time value in `E2:E11`, displayed with `[$-409]h:mm AM/PM;@` | Included among the required pickup/drop-off times. (PDF p. 4) |
| 6 | `ReportedPickupPerformTime` (`Sheet1!F1`) | Reported performance of pickup | Excel time value in `F2:F11`, displayed with `[$-409]h:mm AM/PM;@` | Must be after Reported Pickup Arrive. (PDF pp. 4, 6–9) |
| 7 | `ScheduledDropoffTime` (`Sheet1!G1`) | Scheduled drop-off | Excel time value in `G2:G11`, displayed with `[$-409]h:mm AM/PM;@` | Included among the required pickup/drop-off times. (PDF p. 4) |
| 8 | `ReportedDropoffArriveTime` (`Sheet1!H1`) | Reported arrival at drop-off | Excel time value in `H2:H11`, displayed with `[$-409]h:mm AM/PM;@` | Included among the required pickup/drop-off times. (PDF p. 4) |
| 9 | `ReportedDropoffPerformTime` (`Sheet1!I1`) | Reported performance of drop-off | Excel time value in `I2:I11`, displayed with `[$-409]h:mm AM/PM;@` | Must be after Reported Dropoff Arrive. (PDF pp. 4, 6–9) |
| 10 | `TripLogSignature` (`Sheet1!J1`) | Whether the Trip Log has a signature | Text; every supplied example is `Y` | The guide says Yes or No, while the workbook demonstrates `Y`. The exact accepted token set (`Y`/`N` versus full words) is not stated. (PDF p. 4; `Sheet1!J2:J11`) |

The guide says to enter the Trip Number, Driver Number, Vehicle VIN Number, required pickup/drop-off times, and Trip Log Signature. It does not identify any of the ten columns as optional. The conservative exporter should therefore populate all ten for every submitted claim, but strict per-column requiredness still needs portal verification. (PDF p. 4)

## Portal workflow

1. Sign in to MTM Link with provider credentials and open **Claims**. (PDF p. 1)
2. Select **Bulk Upload**. (PDF p. 2)
3. On the Bulk Upload page, use **Upload** to import the Excel Claims Sheet. (PDF p. 3)
4. MTM Link displays all uploaded rows in **Claims Review**. (PDF p. 5)
5. Resolve rows that are not ready. The blue pencil edits a row, the red X removes it, and the review form permits correcting values such as the Driver/license identifier or Vehicle VIN. Save an edited row. (PDF pp. 6–8)
6. Every row must reach **Ready** before **Continue** is available for submission. (PDF pp. 6, 9)
7. In **Submission Confirmation**, enter a Claim Packet Name and upload the Signature Document(s), then select **Submit**. (PDF p. 10)
8. MTM Link displays packet-level results. The example UI exposes `Success`, `Failed`, `Requires Action`, `Rejected`, and `Date Uploaded`. (PDF p. 11)
9. Return to **Claims** to monitor each Claim Status; the guide says hovering the status icon reveals its text. (PDF p. 12)

## Explicit validation and failure behavior

MTM states four review gates repeatedly in the guide:

1. All claims must be in a ready-to-submit status before the provider can continue.
2. Drop-off times must be after pickup times.
3. `ReportedPickupPerformTime` must be after `ReportedPickupArriveTime`.
4. `ReportedDropoffPerformTime` must be after `ReportedDropoffArriveTime`.

These rules appear on PDF pp. 6–9 (and are partially visible on p. 5). The phrase “drop-off times must be after pickup times” does not specify every pairwise comparison; the exporter should validate the full event sequence conservatively and confirm overnight handling in the portal.

The review screen marks valid rows with a green check and problem rows with a warning indicator. Problem rows can be edited or removed before continuing. The guide illustrates an unrecognized Driver as `unknown driver`, but it does not enumerate portal error messages or error codes. (PDF pp. 5–9)

MTM warns that it will not approve a Claim or Claim appeal when the associated Driver or Vehicle is not approved or is out of compliance with contractual requirements. This is an approval constraint even if the workbook parses successfully. (PDF p. 10)

After submission, packet feedback includes success and failure counts plus requires-action and rejected states. The guide does not say whether successful rows from a partially failed packet remain submitted, whether a corrected packet can reuse a name, or how duplicate Trip Numbers behave. (PDF p. 11)

## Mapping to the current repository

This comparison reflects the current Manifest Import model in `src/Api/Features/ManifestImports/ManifestModels.cs` and the synthetic manifest shape in `tests/Api.IntegrationTests/Fixtures/mtm-manifest.csv`.

| MTM export field | Current source | Gap / decision |
|---|---|---|
| `Trip Number` | `Trip.TripNumber` | Available as broker-original data. |
| `Driver` | Manifest has `Driver Name`, but it is not imported; there is no Driver domain entity or MTM Driver Number | **Missing.** Issue #5 must capture the MTM identifier separately from a display name and retain assignment history. Numeric workbook storage raises a leading-zero question to test. |
| `Vehicle` | `Trip.VehicleType`; manifest also has a `Vehicle` column that is not imported | **Missing.** Vehicle type is not a VIN. Add a provider-owned Vehicle record with its MTM-recognized VIN and assignment relationship. |
| `ScheduledPickupTime` | Planned in the MVP, not yet modeled | **Missing.** Add the provider-owned Scheduled Pickup Time in Dispatch; repeat manifest imports must not overwrite it. |
| `ReportedPickupArriveTime` | None | **Missing.** The agreed one-tap Actual Pickup Time alone cannot satisfy both pickup arrival and pickup performed fields. Decide which Driver action or deterministic policy captures each event. Do not silently duplicate a timestamp without validating that MTM permits it. |
| `ReportedPickupPerformTime` | Planned Actual Pickup Time, not yet modeled | **Missing today.** The future Actual Pickup Time is the likely semantic source, subject to confirmation. Preserve the original Driver event and any correction history. |
| `ScheduledDropoffTime` | `Trip.AppointmentTime` is broker-original appointment time | **Not safely mapped.** The terms differ. Establish a documented rule—possibly appointment time for outbound legs, but not necessarily return/will-call Trips—and keep it separate from broker facts. |
| `ReportedDropoffArriveTime` | None | **Missing.** The agreed one-tap Actual Drop-off Time does not distinguish arrival from performance. |
| `ReportedDropoffPerformTime` | Planned Actual Drop-off Time, not yet modeled | **Missing today.** The future Actual Drop-off Time is the likely semantic source, subject to confirmation and append-only correction history. |
| `TripLogSignature` | None; the MVP spec explicitly deferred signatures unless required by the contract | **Now required by the supplied guide.** Capture whether the Trip Log was signed and retain the evidence/attestation history. Exact accepted workbook tokens remain unresolved. |

The submission step also requires a **Claim Packet Name** and **Signature Document(s)**, neither of which is currently modeled. Because the application will not automate MTM Link, the MVP can generate the Claims Sheet and give the Dispatcher a concise handoff checklist. Whether it should store signature documents is a separate privacy, retention, and security decision; the workbook itself carries only the yes/no field. (PDF p. 10)

An export-readiness rule will need to require, at minimum, a claimable completed/closed Trip, compliant assigned Driver and Vehicle identifiers, the six ordered time values, and a resolved Trip Log signature value. “Claimable completed/closed” is an application inference from the operational model, not wording supplied by these artifacts.

## Unknowns requiring a synthetic portal trial

Do not infer these from the sample workbook:

- Whether `Sheet1` is a required worksheet name and whether extra sheets are rejected.
- Whether header order and capitalization are strictly matched.
- Whether `.xlsx` is the only accepted file type, plus file-size and row-count limits.
- Whether all ten cells are required for every Trip, especially will-call, cancelled, no-show, or cross-midnight Trips.
- Exact time semantics: service timezone, date association, seconds handling, equal timestamps, and overnight ordering.
- The exact accepted `TripLogSignature` tokens.
- Driver Number type/length and whether leading zeroes are significant.
- VIN validation details and whether the portal accepts only preconfigured Driver/Vehicle pairs.
- Duplicate Trip Number behavior and whether an already-claimed Trip can be uploaded again.
- Signature-document file types, count, size limits, naming rules, and whether documents cover a packet or individual Trips.
- Whether successful rows survive a partially failed submission and how requires-action rows are corrected and resubmitted.

Use synthetic Trip, Driver, Vehicle, and signature-document data for this trial. Record the results before implementing production export validation.
