# MDSweep refactoring plan

## Purpose

Refactor MDSweep toward the project and feature organization used by Motion while preserving MDSweep's NEMT behavior, public HTTP contracts, security boundary, data model, and manual MTM file exchange.

This plan is based on:

- [MDSweep issue #19](https://github.com/ahyousif/mdsweep/issues/19)
- [Motion `develop` at `06bc41f749edb6da3853ebd2e576b8ce20cc181a`](https://github.com/verbenum/motion/tree/06bc41f749edb6da3853ebd2e576b8ce20cc181a)
- [MDSweep domain language](../CONTEXT.md), [architecture](ARCHITECTURE.md), ADRs, and MVP research
- The decisions confirmed for this refactor on 2026-08-28

When the sources disagree, use this precedence:

1. Issue #19 and the confirmed decisions in this document
2. MDSweep's domain, workflow, data-safety, and acceptance requirements
3. Motion's implementation patterns

Motion is a structural reference, not a source of MDSweep domain behavior.

## Confirmed decisions

- Adopt Motion's separate API, Application, Domain, and Infrastructure project shape.
- Continue using EF Core and PostgreSQL.
- Keep one MDSweep application database and a separate Keycloak database, as today.
- Use Wolverine for message dispatch, HTTP endpoints, and EF Core unit-of-work behavior.
- Wolverine's transaction middleware is the only middleware work included in this refactor.
- Keep the existing Keycloak backend-for-frontend flow: one `mdsweep` realm, server-side OIDC, HttpOnly application cookie, and Provider organizations.
- Add both TanStack Query and TanStack Table.
- Upgrade Angular to the latest stable release.
- Add Tailwind CSS and bring in only the Spartan UI components needed by current MDSweep workflows.

## Explicit exclusions

Do not copy, redesign, or introduce any of the following while performing this refactor:

- General-purpose or tenant middleware; the Wolverine EF Core transaction integration is the sole exception.
- Authorization rules, policies, role modeling, or Provider-access behavior. Existing authorization must continue to work, but it is not being redesigned.
- Email or invitation email delivery.
- Wolverine durable queues, inboxes, outboxes, message persistence, or outbox database tables.
- Recurring jobs, hosted schedulers, or automation workers.
- Motion's Dapper, FluentMigrator, master/tenant database, repository, or database-per-tenant design.
- Motion's realm-per-tenant and browser-held Keycloak bearer-token flow.
- MTM API integration or MTM portal automation.
- Billing Export behavior while its authoritative MTM contract remains pinned.
- Production migration/backup/restore operations, Keycloak realm administration, or approval for patient-linked data. Those remain part of deployment readiness.

All fixtures, screenshots, logs, and verification data must remain synthetic.

## Motion comparison

| Area | Motion pattern | MDSweep target |
| --- | --- | --- |
| Backend projects | API, Application, Domain, Infrastructure, AppHost, ServiceDefaults, and Utility | Adopt API, Application, Domain, Infrastructure, AppHost, ServiceDefaults, and Web. Do not add Utility until deployment-readiness work requires a separate migration/provisioning executable. |
| Feature flow | Wolverine HTTP endpoint sends an application command/query to a Wolverine handler | Adopt this flow while keeping existing MDSweep routes and response shapes. |
| Persistence | Dapper repositories, FluentMigrator, master database, and tenant databases | Keep EF Core, one application database, feature-owned mappings, and the existing migration chain. Do not add repositories or an `IApplicationDbContext` facade. |
| Unit of work | Custom Dapper transaction middleware | Use Wolverine's official EF Core transaction integration. Do not copy Motion's `UnitOfWork`, connection-factory, or custom middleware classes. |
| Keycloak | Tenant realm routing and SPA bearer tokens | Retain MDSweep's single realm, Provider organizations, server-side OIDC, cookie, and antiforgery flow. |
| Angular organization | `core`, `features`, `layout`, and copied Spartan helm components | Adopt this organization, but generate only the MDSweep components listed below. |
| Spartan/Tailwind | Broad UI library on Tailwind | Use Tailwind 4 and a small, workflow-driven Spartan component set. |
| TanStack | Not present on the compared Motion commit | Add Query and Table because issue #19 and the confirmed decision take precedence. |

Relevant Motion examples include its [solution structure](https://github.com/verbenum/motion/blob/06bc41f749edb6da3853ebd2e576b8ce20cc181a/motion.slnx), [Wolverine bootstrap](https://github.com/verbenum/motion/blob/06bc41f749edb6da3853ebd2e576b8ce20cc181a/src/Motion.Api/Program.cs), [endpoint class](https://github.com/verbenum/motion/blob/06bc41f749edb6da3853ebd2e576b8ce20cc181a/src/Motion.Api/Features/Core/Clients/Create/CreateClientEndpoint.cs), [application handler](https://github.com/verbenum/motion/blob/06bc41f749edb6da3853ebd2e576b8ce20cc181a/src/Motion.Application/Core/Clients/Create/CreateClientHandler.cs), and [Angular/Tailwind setup](https://github.com/verbenum/motion/blob/06bc41f749edb6da3853ebd2e576b8ce20cc181a/src/Motion.Web/src/styles.css).

## Target repository shape

Rename the existing generic project directories so project and namespace boundaries are explicit:

```text
src/
  Mdsweep.Api/
    Features/
      ManifestImports/
      Dispatch/
      DriverWork/
      Identity/
    Program.cs
  Mdsweep.Application/
    Common/
    ManifestImports/
    Dispatch/
    DriverWork/
    Identity/
  Mdsweep.Domain/
    ManifestImports/
    Dispatch/
    DriverWork/
    Identity/
  Mdsweep.Infrastructure/
    Features/
      ManifestImports/
      Dispatch/
      DriverWork/
    Identity/
      Keycloak/
    Persistence/
      Configurations/
      Migrations/
      ApplicationDbContext.cs
    DependencyInjection.cs
  Mdsweep.AppHost/
  Mdsweep.ServiceDefaults/
  Mdsweep.Web/
tests/
  Mdsweep.Api.IntegrationTests/
  Mdsweep.Web.E2ETests/
```

Use these dependency rules:

- `Mdsweep.Domain` references no other MDSweep project.
- `Mdsweep.Application` references `Mdsweep.Domain`.
- `Mdsweep.Infrastructure` references `Mdsweep.Application` and `Mdsweep.Domain`.
- `Mdsweep.Api` references `Mdsweep.Application`, `Mdsweep.Infrastructure`, and `Mdsweep.ServiceDefaults`.
- `Mdsweep.AppHost` references `Mdsweep.Api`.
- Integration tests exercise the application through `Mdsweep.Api` and PostgreSQL.

Keep behavior organized as vertical features inside each project. Do not create a generic shared-kernel project, generic repositories, or one project per feature. Shared code must represent an established cross-feature concept.

### EF-dependent handler placement

Motion places handlers in Application and hides persistence behind repository interfaces. MDSweep must continue to use EF Core directly without inventing a persistence seam. Therefore:

- Application owns Wolverine command/query contracts, results, pure policies, and orchestration that has no infrastructure dependency.
- Infrastructure owns handlers that need the concrete `ApplicationDbContext`, EF queries, mappings, file-format adapters, or external services.
- Domain owns entities, value objects, state transitions, and business rules that do not depend on EF or HTTP.
- API owns Wolverine HTTP endpoint classes, HTTP requests/responses, route metadata, and server-resolved identity context.

This is the deliberate EF Core adaptation of Motion's project shape. It avoids both an inverted Application-to-Infrastructure reference and a repository abstraction with only one implementation.

## Required backend changes

### 1. Establish project and build boundaries

- Add `Mdsweep.Application`, `Mdsweep.Domain`, and `Mdsweep.Infrastructure` projects.
- Rename `src/Api`, `src/AppHost`, and `src/ServiceDefaults` to the explicit project directories shown above.
- Rename `src/Web` to `src/Mdsweep.Web`.
- Update `Mdsweep.slnx`, project references, root namespaces, launch profiles, AppHost generated project references, test references, and all path-based scripts.
- Add `Directory.Build.props` for shared .NET 10, nullable, implicit-usings, formatting, and warning settings.
- Add `Directory.Packages.props` and centrally manage .NET package versions. Do not copy unrelated Motion dependencies.
- Keep the current .NET 10 and Aspire deployment shape.

Required backend packages are limited to the existing application packages plus:

- `WolverineFx`
- `WolverineFx.Http`
- `WolverineFx.EntityFrameworkCore`

Do not add `WolverineFx.Postgresql` unless a later accepted issue authorizes durable Wolverine storage. EF Core continues to use `Npgsql.EntityFrameworkCore.PostgreSQL` for application persistence.

### 2. Configure Wolverine endpoints and discovery

- Register Wolverine on the host and include the Application and Infrastructure assemblies in handler discovery.
- Register Wolverine HTTP support and replace the static `MapManifestImports`, `MapDispatch`, `MapDispatchManagement`, and `MapDriverWork` registrations incrementally with `MapWolverineEndpoints`.
- Use public `*Endpoint` classes and public endpoint methods because Wolverine generates endpoint code at runtime.
- Use `[WolverineGet]`, `[WolverinePost]`, `[WolverinePut]`, or the corresponding verb attribute while preserving every existing route, verb, status code, request body, response body, and file-upload contract.
- Keep Identity's OIDC challenge, logout, provider-context, and antiforgery endpoints behaviorally unchanged. Convert them only where Wolverine does not disturb the ASP.NET Core authentication result flow.
- Resolve Provider/App User context on the server before constructing an internal command. Never add `ProviderId` to a public request merely to simplify handler dispatch.
- Remove each old endpoint mapper only after the equivalent Wolverine endpoint passes integration tests.

Do not make one method both a Wolverine HTTP endpoint and a message handler. Follow Motion's endpoint-to-message separation so HTTP binding concerns do not leak into command handlers.

### 3. Use EF Core as the Wolverine unit of work

Use the official [Wolverine EF Core integration](https://wolverinefx.io/guide/durability/efcore/) rather than Motion's Dapper `UnitOfWork` implementation:

- Register `ApplicationDbContext` with Wolverine's EF integration or register the context normally and enable `UseEntityFrameworkCoreTransactions()`.
- Mark command handlers that mutate state with Wolverine's built-in `[Transactional]` attribute. Do not use automatic transaction application because read handlers also depend directly on `ApplicationDbContext`; queries must not open write transactions.
- Let Wolverine call `SaveChangesAsync` for transaction-managed handlers; remove handler-local saves after each handler is covered by the transaction integration.
- Keep explicit saves for non-Wolverine bootstrap work such as the synthetic development seeder.
- Verify that a successful command commits all changes and a thrown exception commits none.
- Do not configure durable local queues, PostgreSQL message persistence, inbox/outbox storage, sagas, or Wolverine-managed schema migrations.
- Continue using checked-in EF Core migrations rather than Wolverine/Weasel schema management.

The migration must include a focused integration test proving commit and rollback through a public HTTP endpoint. Inspect the database after a failed request, not only the returned status code.

### 4. Preserve EF Core and database history

- Move `ApplicationDbContext`, configurations, migration classes, designer files, and the model snapshot into `Mdsweep.Infrastructure` without changing migration IDs.
- Configure the migrations assembly explicitly after the move so the existing migration chain remains the baseline for a fresh database.
- Do not squash, rename, regenerate, or replace the existing migrations solely because namespaces or project paths changed.
- Do not create legacy-data backfills or production migration runbooks in this refactor.
- Keep one `mdsweep` application database and the current separate `keycloak` database on the same PostgreSQL server resource.
- Preserve every existing table, key, index, concurrency/idempotency constraint, and append-only history record.

### 5. Move feature code without changing domain behavior

| Current area | Target ownership |
| --- | --- |
| `ManifestModels.cs` domain state and row-disposition rules | Domain `ManifestImports` |
| CSV/XLSX readers and tabular normalization | Infrastructure `ManifestImports` adapter |
| Manifest command/query contracts and result models | Application `ManifestImports` |
| Manifest Wolverine endpoints | API `Features/ManifestImports/<Behavior>` |
| Manifest EF handlers and read models | Infrastructure `Features/ManifestImports/<Behavior>` |
| Dispatch scheduling and assignment entities/rules | Domain `Dispatch` |
| Dispatch commands, queries, and results | Application `Dispatch` |
| Dispatch EF handlers/read models | Infrastructure `Features/Dispatch` |
| Driver event, correction, conflict, ordering, and idempotency rules | Domain `DriverWork` |
| Driver commands, queries, results, and clock contract | Application `DriverWork` |
| Driver EF handlers and system clock | Infrastructure `Features/DriverWork` |
| Provider, App User, and Provider Membership domain records | Domain `Identity` |
| OIDC/cookie/antiforgery HTTP boundary | API `Features/Identity` |
| Keycloak administration client and development seeding | Infrastructure `Identity/Keycloak` and `Persistence` |

Preserve these invariants during every move:

- Broker-original Trip facts remain separate from Provider overrides.
- Operational History remains append-only.
- Repeat imports remain idempotent and do not erase local changes.
- Journey and Trip assignments retain previous assignments.
- Driver device capture time and server receipt time remain distinct.
- Offline Driver actions remain idempotent.
- Manual MTM manifest input and billing-file output remain in place.

### 6. Keep the Keycloak BFF unchanged

- Keep one `mdsweep` realm per environment and map Providers to Keycloak Organizations.
- Keep ASP.NET Core authorization-code login, server-side token handling, HttpOnly `.Mdsweep.Auth` cookie, and antiforgery cookie/header behavior.
- Keep Angular same-origin and token-free.
- Do not add `keycloak-angular`, `keycloak-js`, browser token storage, tenant realm selection, or Motion's JWT configuration cache.
- Keep the separate Keycloak database and the current AppHost realm import for local synthetic development.
- Move Keycloak code only as required by the project split; do not redesign administration or authorization.

### 7. Update API and AppHost composition

- Reduce API `Program.cs` to service registration, authentication/antiforgery configuration, Wolverine configuration, pipeline setup, and endpoint mapping.
- Move EF, feature handlers, Keycloak administration, and system-clock registrations behind `Mdsweep.Infrastructure.DependencyInjection` extensions.
- Update AppHost project references and `../Mdsweep.Web` paths while retaining the `api`, `web`, `mdsweep`, `keycloak-db`, and Keycloak resource behavior expected by deployment.
- Keep startup migration and synthetic development seeding behavior until deployment readiness defines a different operational migration process.
- Do not introduce Motion's Utility migration process as part of issue #19.

## Required frontend changes

### 1. Upgrade Angular safely

The latest stable release verified on 2026-08-28 is Angular 22.1 (`@angular/core` 22.1.4 and `@angular/cli` 22.1.6). Angular's [official update guidance](https://angular.dev/update) requires major-version updates one at a time.

- Upgrade Angular 20 to 21, run migrations/build/tests, then upgrade 21 to 22 and repeat.
- Align Angular framework packages, Angular CDK, service worker, build tooling, TypeScript, and Zone.js using Angular's update schematics.
- Recheck the npm `latest` tag immediately before implementation and pin the latest stable patch available then.
- Preserve the PWA manifest, service-worker registration, `ngsw-config.json`, proxy rules, production static-file publication, and installable browser experience.
- Do not combine the Angular major upgrades with feature decomposition in the same commit. Establish a passing Angular 22 baseline first.

### 2. Split the monolithic Angular application by workflow

Replace the current root component containing every workflow with route-level features patterned after Motion:

```text
src/Mdsweep.Web/src/app/
  core/
    api/
    auth/
    query/
  features/
    manifest-imports/
      data-access/
      pages/
      ui/
    dispatch/
      data-access/
      pages/
      ui/
    driver-work/
      data-access/
      pages/
      ui/
  layout/
  ui/
  app.config.ts
  app.routes.ts
```

- Add lazy feature routes for Manifest Import, Dispatch, and Driver Work.
- Keep the authenticated role decision and Provider-context selection server-backed.
- Keep interface strings in localization-ready resources rather than embedding new strings throughout components.
- Use `OnPush` change detection for new components.
- Keep the dispatcher desktop day-board and driver mobile next-action surfaces deliberately different.

### 3. Add TanStack Query

Install `@tanstack/angular-query-experimental`. Version 5.102.8 was current when this plan was written. Its [official Angular package remains experimental](https://tanstack.com/query/latest/docs/framework/angular/installation), so pin an exact patch version and upgrade it deliberately.

- Register one `QueryClient` in `app.config.ts` with production-safe retry defaults.
- Create feature-owned query-key factories and query/mutation options; do not build one generic data service.
- Include the server-resolved Provider/App User context in keys for Provider-owned data, and clear relevant cache state on logout or context change.
- Use queries for Provider context, service-day Trips, Driver Trips, conflicts, histories, Drivers, and Vehicles.
- Use mutations for manifest preview/apply, scheduled-time changes, assignments, Driver events/corrections, and management actions.
- Invalidate only affected keys after a successful mutation. Do not reload the entire application.
- Do not automatically retry authentication, authorization, validation, or conflict responses.
- Keep actionable server error messages visible in the owning workflow.

Do not replace the durable `DriverActionQueue` with TanStack's in-memory mutation cache. The dedicated queue must still survive browser restarts, retain action IDs/device times, and expose `Waiting to sync` and `Needs attention`. Do not persist the general Query cache or expand patient-linked browser storage. Clear the limited Driver cache and queued data on logout according to the existing security workflow.

### 4. Add TanStack Table

Install `@tanstack/angular-table`; version 9.2.4 was current when this plan was written. TanStack Table is headless, so combine its state/row model with semantic table markup and the locally generated Spartan table styles.

Use it for:

- The dispatcher day board: useful sorting, visible filters, Journey row grouping, Journey/Trip selection, and column visibility.
- Manifest preview: Ready/Warning/Blocked filtering and review-focused rows where it improves the existing flow.

Do not use it for Driver cards or simple lists. Enable only the Table v9 features the screens need rather than its stock all-features bundle. Preserve keyboard access, semantic headers, text status labels, sticky headers, selected-row detail behavior, and the dispatch UX's filter/scroll continuity. Keep the current client-side service-day data model unless a separately measured performance problem requires server pagination.

### 5. Add Tailwind and a minimal Spartan UI set

Spartan requires Tailwind CSS v4. Follow its [official installation model](https://www.spartan.ng/documentation/installation): install the maintained brain package and copy selected helm component code into MDSweep for local ownership.

- Add `tailwindcss`, `@tailwindcss/postcss`, and the PostCSS configuration required by Angular.
- Add `@spartan-ng/brain`, `@spartan-ng/cli`, and aligned `@angular/cdk` packages.
- Initialize Spartan and add its Tailwind preset/theme variables to global styles.
- Preserve the existing MDSweep visual identity and accessible state colors; do not copy Motion's theme blindly.
- Generate and commit only components required by current workflows.

Initial allowed component set:

- Alert
- Badge
- Button
- Card
- Checkbox
- Input
- Label
- Native Select
- Sheet, for the dispatcher detail panel
- Table
- Tabs, for Needs review/Ready/All
- Spinner or Skeleton, choosing only one loading treatment

Keep the manifest file picker as an accessible native file input with Spartan/Tailwind styling. Add another Spartan component only when a concrete screen requires it and review the copied code as application source. Do not copy Motion's complete `libs/ui` tree.

Driver primary actions must retain at least 44-by-44 CSS-pixel targets, persistent labels, explicit offline state, and keyboard/focus behavior. Tailwind adoption must not reduce the accessibility requirements in the dispatch UX research.

## Verification changes

### Backend characterization before moving code

Add or complete PostgreSQL-backed integration coverage for the public interfaces being moved:

- Manifest preview and apply, including repeat-import idempotency and preserved Provider overrides.
- Service-day reads and scheduled-pickup history.
- Journey assignment, Trip reassignment, and assignment history.
- Driver event ordering, offline action idempotency, corrections, and sync conflicts.
- Provider scoping and the existing authentication/antiforgery flow without redesigning authorization.
- Wolverine EF transaction commit on success and rollback on failure.

Tests must continue to use synthetic manifests and observable HTTP/database outcomes. Do not replace PostgreSQL with an EF in-memory provider.

### Frontend coverage

- Update focused Angular tests to the Angular 22 test builder selected by the official migration.
- Test query-key isolation, invalidation after mutations, non-retryable errors, and cache clearing on context changes.
- Test TanStack Table filtering, sorting, Journey selection, and restoration of dispatch state.
- Retain tests for offline queue persistence and synchronization independently of TanStack Query.
- Add one Playwright smoke path covering synthetic manifest import, dispatch assignment, and Driver completion online; add the offline restart/reconnect path when the refactored PWA queue is wired.

### Required commands

The completed refactor must pass:

```text
dotnet restore Mdsweep.slnx
dotnet build Mdsweep.slnx --configuration Release --no-restore
dotnet test Mdsweep.slnx --configuration Release --no-build
npm ci --prefix src/Mdsweep.Web
npm run build --prefix src/Mdsweep.Web
npm test --prefix src/Mdsweep.Web -- --watch=false
```

Also run the focused Playwright smoke test and `aspire run` smoke verification for PostgreSQL, Keycloak, API, and Web composition.

## Repository and deployment updates

Update every path-sensitive consumer of the renamed projects:

- `Mdsweep.slnx`
- `.github/workflows/ci-deploy.yml`, including npm cache paths, working directories, and the `aspire deploy --apphost` path
- AppHost project references and Web working directory/container-file publication
- Integration-test project references and linked Keycloak realm fixture
- VS Code launch/task settings if project paths are added later
- `docs/ARCHITECTURE.md`, `docs/specs/mvp.md`, `AGENTS.md`, and `docs/production-deployment.md`
- Root README with build/run/test entry points

Add an ADR for the layered project split and Wolverine/TanStack/Spartan foundation because these choices supersede the current single-API-project source layout and the earlier deferral of Wolverine. The ADR must explicitly retain EF Core, the single application database, manual MTM exchange, the Keycloak BFF, and the ignored subsystems.

Do not change Azure resource names, rotate secrets, deploy patient-linked data, or infer a production upgrade procedure as part of the refactor.

## Buildable implementation sequence

Each step must leave the repository buildable and avoid two parallel implementations owning the same endpoint.

1. **Characterize the current public behavior.** Add the missing integration tests and capture the current route/response contracts with synthetic data.
2. **Upgrade Angular in isolation.** Move 20 to 21 and then 22, retaining the current UI and a green build after each major.
3. **Create the .NET project skeleton.** Add central build/package files, new projects, dependency rules, renamed paths, and CI/AppHost updates without moving feature behavior yet.
4. **Move Domain and EF infrastructure.** Relocate entities/rules, DbContext/configurations/migrations, Keycloak adapter, seeder, and clock. Prove that the existing database model snapshot is unchanged.
5. **Add Wolverine foundation.** Configure discovery, HTTP endpoints, and EF transactions without durable message storage. Convert Manifest Preview as the tracer endpoint and prove commit/rollback behavior.
6. **Migrate one vertical behavior at a time.** Finish Manifest Import, then Dispatch, then Driver Work. Preserve routes and delete each old mapper as its replacement lands. Move Identity only as needed to complete the project split.
7. **Add frontend foundations.** Install Tailwind/Spartan, the exact initial component set, TanStack Query, and TanStack Table. Establish routing, core providers, feature folders, and a passing shell.
8. **Move frontend workflows vertically.** Manifest Import first, Dispatch day board second, Driver Work last so the offline queue receives focused regression testing.
9. **Remove superseded code and CSS.** Delete the monolithic root workflow, unused handcrafted component CSS, obsolete endpoint mappers, direct per-handler saves covered by Wolverine, and unused packages.
10. **Update architecture records and run final verification.** Complete the ADR/docs, CI-equivalent commands, Playwright smoke path, and Aspire composition check.

## Completion criteria

The refactor is complete only when all of the following are true:

- The solution has the agreed API/Application/Domain/Infrastructure project boundaries and dependency direction.
- Existing public API routes, response contracts, Keycloak BFF behavior, Provider scoping, and antiforgery behavior are preserved.
- Mutating Wolverine handlers commit once on success and roll back completely on failure through EF Core.
- No Wolverine durable storage, outbox, job, email, general middleware, or authorization redesign has entered the change.
- The existing EF migration chain remains valid and targets the same single MDSweep application database; Keycloak retains its separate database.
- Angular is on the latest stable release available when implementation begins.
- TanStack Query owns server-state fetching/mutations without replacing the durable Driver offline queue.
- TanStack Table powers only the data-dense dispatcher/import tables that need it.
- Tailwind and only the approved Spartan components are present.
- Manifest import, dispatch, Driver completion, PWA/offline behavior, Operational History, and manual MTM exchange retain their acceptance behavior.
- All .NET, Angular, Playwright, and Aspire checks pass using synthetic data.
- Architecture, agent guidance, CI, and deployment documentation match the new paths and responsibilities.
