# MTM provider privacy, security, integration, and records requirements

_Research date: 2026-08-12. This is product research, not legal advice. Sources are primary/official only. MTM requirements vary by program, state, health plan, and the provider's signed contract; the actual contract controls._

## Executive conclusion

The public record is enough to reject two unsafe assumptions:

1. MTM publicly says transportation providers must remain HIPAA compliant, protect PHI, and train every employee annually. An older, Mississippi-specific MTM provider handbook also binds confidentiality obligations to agents and subcontractors and requires service records to be retained for ten years or longer when law or client requirements demand it.
2. MTM supports approved third-party routing/scheduling/dispatch (RSD) integrations through an API. Public MTM Terms of Use do not expressly say “bots” or “scraping,” but they prohibit exploiting MTM websites without express written consent and allow MTM to terminate access. Therefore, Playwright automation against MTM Link should **not** be treated as authorized merely because a provider supplies credentials. Obtain written approval or an approved API/integration path first.

The exact privacy/security addendum, subcontractor permission, incident-notification deadline, data-return/destruction rules, audit rights, state-specific retention period, portal clickwrap terms, and claims bulk-upload specification were not publicly located. The provider should request these documents from its Vendor Account Manager before real MTM data enters the proposed SaaS.

## What MTM publicly requires

### Provider contracts and program-specific rules control

MTM says every provider must follow “Provider Guidelines,” which are incorporated into each provider contract. It also says every employee of each provider must complete HIPAA and fraud/waste/abuse training annually. This establishes that public web pages are not the complete rule set. [MTM provider onboarding](https://www.mtm-inc.net/driverswanted/) (accessed 2026-08-12).

MTM's current Texas provider page likewise says onboarding includes contracting, credentialing, and training, and directs existing providers to their dedicated Vendor Account Manager. [MTM Texas transportation providers](https://www.mtm-inc.net/texas/transportation-providers/) (accessed 2026-08-12).

**Implication:** obtain the client's executed Provider Service Agreement, all schedules/amendments, Provider Guidelines, health-plan/state exhibits, privacy/security or BAA addenda, and current MTM Link terms. A handbook from another state cannot safely substitute for them.

### HIPAA, PHI confidentiality, and subcontractors

An official MTM Mississippi NET Provider Handbook (version 2.0, dated 2018) says MTM mandates that transportation providers remain HIPAA compliant and follow PHI restrictions. It requires activities under the agreement to comply with HIPAA/HITECH and 45 C.F.R. Parts 160, 162, and 164 as applicable. It also says each party must treat accessible data as confidential and not disclose it to a third party without specific written consent; those duties survive termination and bind agents, employees, successors, assigns, and subcontractors. See pp. 5–6 of the [MTM Mississippi Transportation Provider Handbook](https://www.mtm-inc.net/wp-content/uploads/2019/02/MS-Transportation-Provider-Handbook-v2.pdf) (accessed 2026-08-12).

**Limits:** this is an older Mississippi program handbook, not proof of the exact terms in the Arizona provider's current agreement. Its language is strong evidence that subcontracting a SaaS that receives MTM trip data is a contractual issue requiring review and possibly MTM's written consent.

MTM separately explains that its own arrangements with Medicaid/managed Medicaid clients include BAAs permitting MTM to receive certain member PHI. That statement concerns MTM's relationship with its clients; it does **not** establish that a transportation provider or the provider's SaaS vendor is covered by that same BAA. [MTM facility trip verification](https://www.mtm-inc.net/facility-trip-verification/) (accessed 2026-08-12).

Official HHS guidance says a software vendor that hosts or accesses PHI for a regulated customer is ordinarily a business associate, and a subcontractor that creates, receives, maintains, or transmits PHI for another business associate is also a business associate. HHS also says a cloud provider maintaining ePHI generally needs a HIPAA-compliant BAA even when the data is encrypted and the cloud provider lacks the key. [HHS: Is a software vendor a business associate?](https://www.hhs.gov/hipaa/for-professionals/faq/256/is-software-vendor-business-associate/index.html) and [HHS cloud computing guidance](https://www.hhs.gov/hipaa/for-professionals/special-topics/health-information-technology/cloud-computing/index.html) (accessed 2026-08-12).

**Inference, not a conclusion about this client's legal status:** a hosted SaaS that stores MTM manifests and lets staff/drivers view and update identifiable trip data will create, receive, maintain, and transmit health-related identifiable information. The provider and counsel should determine the exact HIPAA chain and execute any required BAA/subcontractor agreement before production use. The SaaS's own subprocessors that touch ePHI may need equivalent restrictions. HHS's official sample terms require safeguards, incident reporting, return/destruction, and downstream subcontractor restrictions. [HHS business-associate contracts](https://www.hhs.gov/hipaa/for-professionals/covered-entities/sample-business-associate-agreement-provisions/index.html) (accessed 2026-08-12).

### Record retention

The Mississippi handbook requires the transportation provider to maintain all records concerning services under its NET Services Agreement for **ten years**, or longer if applicable law, regulation, or client requirements require it. See p. 19 of the [MTM Mississippi Transportation Provider Handbook](https://www.mtm-inc.net/wp-content/uploads/2019/02/MS-Transportation-Provider-Handbook-v2.pdf) (accessed 2026-08-12).

**Implication:** a one-week operational history is plainly inadequate under that program. For this product, do not hard-code “ten years” as a universal MTM rule. Make retention policy configurable, preserve immutable audit evidence, and determine the Arizona/client-specific period from the signed documents. Backups and exports must follow the same retention/deletion policy; “delete from the UI” is not complete deletion.

### Driver workflow and evidence likely needed for claims

MTM's handbook describes the provider portal as supporting trip download, driver assignment, and claims. It says the claim view includes appointment date, assignment, trip number, driver/vehicle, and member signature; missing information is flagged; driver-app data auto-fills much of the claim; and a claims packet should include signature plus trip pickup and drop-off times. It also describes GPS data, pickup/drop-off timestamps, and signature capture in the driver app. See pp. 33–34 of the [MTM Mississippi Transportation Provider Handbook](https://www.mtm-inc.net/wp-content/uploads/2019/02/MS-Transportation-Provider-Handbook-v2.pdf) (accessed 2026-08-12).

The public MTM Link driver guide documents a more detailed event sequence: depot out, arrive pickup, perform pickup/cancel/no-show, participant signature, arrive drop-off, perform drop-off, assignment-change acknowledgement, and depot in. It warns that arrival must precede performed pickup for proper GPS tracking. [MTM Link Driver App Reference Guide](https://www.mtm-inc.net/wp-content/uploads/2018/12/MTM-Link-Driver-App-Reference-Guide.pdf) (accessed 2026-08-12).

MTM's current Wisconsin provider page says all trips must be electronically tracked and documented as proof of completion or cancellation, using the MTM Link Driver App or Provider Portal. It also says MTM does not reimburse no-shows. [MTM Wisconsin transportation providers](https://www.mtm-inc.net/wisconsin/providers/) (accessed 2026-08-12).

**Product implication:** recording only “actual pickup” and “actual drop-off” may not reproduce the evidence MTM expects in the client's program. Before replacing the legacy driver tool, verify whether arrival events, GPS, signature, cancellation/no-show reason, driver, vehicle, mileage, and depot events are mandatory for claims or performance reporting.

### A/B legs, changes, and operational accuracy

MTM's public portal tips say all trips are assigned as A and B legs and updates must reflect each leg. Providers can reassign, cancel, or turn back multiple trips at once; same-day turnbacks are not allowed; providers should review future trips daily; pickup time should be confirmed with the participant one day before transport; and the assigned mode should not be changed. [MTM Link Portal Helpful Tips](https://www.mtm-inc.net/wp-content/uploads/2018/12/MTM-Link-Helpful-Tips-for-TPs.pdf) (accessed 2026-08-12).

The Mississippi handbook says a provider finding incorrect price, service level, mileage, ZIP codes, or other trip data in the electronic trip download must contact its Provider Management Representative before performing the trip. See p. 36 of the [handbook](https://www.mtm-inc.net/wp-content/uploads/2019/02/MS-Transportation-Provider-Handbook-v2.pdf) (accessed 2026-08-12).

**Implication:** retain the broker-original record, treat each leg independently while grouping them as a journey, and do not let local operational overrides silently rewrite MTM facts. Build an explicit “contact MTM / awaiting broker correction” state.

## Automation, scraping, and supported integration

MTM's public Terms of Use apply to its websites, applications, content, and related services. They restrict secure areas to authorized users, prohibit reproducing or otherwise exploiting MTM websites without express written consent, prohibit reverse engineering, and prohibit unauthorized access or impairment. MTM also reserves the right to terminate access and monitors for unauthorized activity. [MTM Terms of Use](https://www.mtm-inc.net/terms-of-use/) (accessed 2026-08-12).

The terms do not use the words “bot,” “robot,” “scrape,” “automated access,” or “Playwright” in the public text reviewed. Therefore:

- **Public fact:** there is no explicit public permission for browser automation.
- **Public fact:** exploitation without express written consent is prohibited, and access can be terminated.
- **Inference:** unattended Playwright login, data extraction, or claim submission creates material contractual and operational risk. Provider credentials alone do not demonstrate MTM consent to automate.
- **Required validation:** inspect any portal-specific clickwrap terms after login and obtain written authorization from MTM for the intended automation. Do not attempt to evade MFA, CAPTCHA, access controls, rate limits, or monitoring.

MTM publicly advertises an approved alternative: API integration with third-party RSD products. Its current provider pages state that providers using their own dispatch software can connect through an API. MTM has announced preferred integrations with RoutingBox, TripMaster, and RouteGenie that exchange trip changes, GPS/status events, and claims data. [MTM RoutingBox integration](https://www.mtm-inc.net/routingbox-integrates-with-the-mtm-link-platform-via-next-generation-api/), [MTM preferred RSD integrations](https://www.mtm-inc.net/mtm-expands-mtm-link-integrations-to-include-two-new-preferred-rsd-partners/), and [MTM Rhode Island transportation providers](https://www.mtm-inc.net/rhode-island/transportation-providers/) (accessed 2026-08-12).

**Recommendation:** ask MTM how a new RSD vendor becomes approved and whether the client's contract/program can enable API access. The earlier belief that “there is no API integration with MTM” is not universally true; public evidence shows an API exists, though it may not be open, self-service, or available to this provider.

## Bulk claims/upload

Public official sources confirm that MTM Link supports provider claims and that approved RSD integrations can automatically transfer claims data after completion. The older handbook describes claims entry and attaching a claims packet, but it does not publish a current bulk-upload CSV/XLSX schema, validation rules, or rejection codes. [MTM preferred RSD integrations](https://www.mtm-inc.net/mtm-expands-mtm-link-integrations-to-include-two-new-preferred-rsd-partners/) and pp. 33–34 of the [MTM Mississippi handbook](https://www.mtm-inc.net/wp-content/uploads/2019/02/MS-Transportation-Provider-Handbook-v2.pdf) (accessed 2026-08-12).

No public, official bulk-claim upload specification was located in this research. The provider's training, portal download/template, or Vendor Account Manager is likely the authoritative source. Do not infer the schema from screenshots or generate production claims until MTM validates a test file and explains duplicate/retry behavior.

## Questions and documents to request from MTM

Ask the client's dedicated Vendor Account Manager in writing:

1. May the provider use a new third-party RSD SaaS to store and process MTM trip/member data? Is prior written approval required?
2. Must MTM review or approve the vendor, hosting environment, security controls, or subcontractors?
3. Is a BAA, downstream BAA, data-use agreement, security addendum, or amendment required between MTM, the provider, and/or the SaaS vendor?
4. What current Provider Guidelines and state/health-plan exhibits apply to this provider? Request the full executed agreement and all amendments.
5. What security controls, audit rights, cyber-insurance limits, breach/security-incident deadlines, and data-location restrictions apply?
6. What is the exact records-retention period, and which claim, GPS, signature, dispatch, communication, and audit records must be retained?
7. May the SaaS access MTM Link through browser automation using the provider's account? If yes, obtain the permitted scope and operational constraints in writing.
8. Can this provider use MTM's RSD API? How does a new software vendor become an approved integration partner, and is a sandbox/test environment available?
9. Provide the current bulk-claims upload template, field definitions, accepted formats, validation rules, attachment/signature requirements, maximum batch size, duplicate handling, corrections/voids, rejection codes, and test procedure.
10. Which driver events are mandatory in this program: acknowledge, en route, arrive pickup, perform pickup, signature, GPS, arrive drop-off, perform drop-off, mileage, cancellation/no-show reason, and depot events?

## Product decisions safe to make now

- Continue development only with synthetic data.
- Design for individual accounts, least-privilege driver access, audit history, broker-original values plus operational overrides, encryption, backups, and configurable long-term retention.
- Keep the MTM import as a user-initiated file workflow for the pilot.
- Keep claims submission and Playwright portal automation behind disabled feature boundaries until MTM provides the controlling documents and written authorization.
- Model all claim/evidence events even if the MVP initially exposes fewer buttons; otherwise replacing the current site may destroy evidence needed later.
- Treat third-party maps, monitoring, support, backups, email/SMS, and cloud hosting as potential PHI subprocessors and review their agreements before production.

## Unresolved gaps

- The client's state, health plan, and exact MTM contracting entity were not independently confirmed from public documents.
- The executed Provider Service Agreement, Provider Guidelines, BAA/privacy/security addendum, and portal clickwrap terms are provider-only or were not publicly found.
- No public current MTM bulk-claims schema or training was found.
- No public onboarding procedure or commercial terms for a new MTM RSD/API integration partner were found.
- No public MTM document established whether browser automation is categorically forbidden; the public terms make proceeding without express written consent unsafe.
- The applicable incident-reporting time, breach-notification time, security audit standard, cyber-insurance requirement, data residency rule, and Arizona-specific retention period remain unknown.
