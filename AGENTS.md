# Repository Guidance

Build the smallest complete workflow that replaces manual manifest processing. Keep changes inside the issue's vertical slice and preserve the manual MTM file exchange at both ends.

Favor delivery over ceremony. For this two-developer MVP, create only buildable issues that prevent duplicated work or preserve acceptance criteria; add process only after a concrete coordination problem appears.

## Current delivery state

The repository has a shared Azure production-shaped environment in `rg-mdsweep-prod`, but it is not approved for patient-linked data. Local and deployed data must remain synthetic until deployment readiness is completed.

- Treat the current EF Core migration set as the baseline for a new database; do not add speculative legacy-data backfills or production-operational runbooks.
- Before any use of non-synthetic data, complete a deployment-readiness issue that defines the database migration/backup/restore procedure, Keycloak realm administration, and data-safety approval.
- Treat `rg-mdsweep-prod` as deployment infrastructure validation only until that issue is accepted. Promote later schema/data changes through an explicit tested upgrade path.

## Agent skills

### Issue tracker

Issues live in GitHub under `ahyousif/mdsweep`; accepted work is added to user project 3. See [docs/agents/issue-tracker.md](./docs/agents/issue-tracker.md).

### Triage labels

Use the five canonical triage labels mapped without renaming. See [docs/agents/triage-labels.md](./docs/agents/triage-labels.md).

### Domain docs

This is a single-context repository using root `CONTEXT.md` and `docs/adr/`. See [docs/agents/domain.md](./docs/agents/domain.md).

## Read when relevant

- Read [CONTEXT.md](./CONTEXT.md) before naming domain types, statuses, commands, or UI labels.
- Read [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) before adding a module, seam, dependency, project, or cross-feature abstraction.
- Read [docs/adr/](./docs/adr/) before changing product scope, deployment shape, or legacy-site strategy.
- Read [docs/research/dispatch-ux.md](./docs/research/dispatch-ux.md) when changing manifest import, the dispatch board, Driver interactions, offline behavior, accessibility, or localization readiness.
- Read [docs/research/mtm-provider-requirements.md](./docs/research/mtm-provider-requirements.md) when changing MTM integration, claims evidence, retention, or production handling of MTM data.

## Working rules

- Organize application code by vertical feature under `src/Api/Features`; keep rules, persistence mapping, validation, and endpoints close to the behavior they implement.
- Use EF Core directly inside a feature. Introduce a seam when multiple adapters are real, including a production adapter and a materially different deterministic test adapter.
- Test through the feature's interface and observable database or HTTP outcomes. Use PostgreSQL for persistence integration tests.
- Preserve broker-original Trip facts, provider overrides, and append-only operational history as distinct data.
- Make repeat imports and offline Driver actions idempotent.
- Keep Billing Export pinned until an authoritative MTM bulk-upload contract is available.
- Use Playwright for this application's end-to-end tests. Add MTM portal automation only after written authorization and a defined durable workflow.

## Data safety

- Use synthetic manifests in source control, tests, screenshots, logs, issues, and pull requests.
- Keep names, Medicaid identifiers, birth dates, phone numbers, street addresses, appointment details, and other patient-linked data out of repository history and diagnostic output.
- Treat the root MTM export as local reference data only; replace it with a synthetic fixture before publishing sample data.

## Completion

A feature is complete when its acceptance criteria pass through its public interface, authorization is covered, relevant history is retained, failures are actionable to the user, and the repository's formatting, tests, and build checks pass.
