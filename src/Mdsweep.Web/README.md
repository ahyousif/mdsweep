# MDSweep Web

This project is the Angular 22 PWA for MDSweep's Trips workflows. The preferred
development entry point is the repository's Aspire AppHost, which starts this project after the
API, PostgreSQL, and Keycloak are ready. See the [root README](../../README.md) for complete setup,
credentials, and troubleshooting.

## Run with the application

From the repository root in the Dev Container:

```bash
npm ci --prefix src/Mdsweep.Web
aspire run
```

Open <http://localhost:4200>. Aspire injects the API endpoints and exposes the API on
<http://localhost:5080>.

## Run the Web project alone

Use this only when the API and its PostgreSQL and Keycloak dependencies are already running:

```bash
cd src/Mdsweep.Web
npm ci
npm start
```

The Angular development server uses `proxy.conf.json` to proxy `/api` and `/signin-oidc` to
`http://localhost:5080`.

## Build and test

```bash
cd src/Mdsweep.Web
npm run build
npm test -- --watch=false
```

Tests use Angular's unit-test builder with Vitest. The production build emits the PWA to
`dist/web/browser`; the AppHost publishes those files with the API for deployment.

## Structure

```text
src/app/
  core/                     Authentication session and shared API behavior
  shell/                    Authenticated application shell
  features/trips/           All Trips, My Trips, and Trip Import workflows
  ui/                       Minimal generated Spartan primitives
```

Angular code is organized by product capability. Administrator, Dispatcher, and Driver roles
authorize routes and actions; they do not define top-level domain feature folders. Trips owns All
Trips, My Trips, and Trip Import experiences. TanStack Query owns non-persisted server state and
invalidation. TanStack Table powers the dense Trip table. The Driver action queue remains a separate durable browser
workflow and must not be replaced by or persisted through the general query cache.

Authentication remains server-owned: Angular calls same-origin endpoints with the ASP.NET Core
session cookie and antiforgery token. It does not receive, store, or refresh Keycloak tokens.
