# MDSweep

MDSweep replaces manual MTM Manifest processing with one synthetic-data workflow for
maintaining Passengers and Trips from Manifest receipt through planning, performance, review,
and manual billing-file exchange. It is a .NET 10 modular monolith with an Angular 22 PWA,
PostgreSQL, Keycloak, Wolverine HTTP endpoints, and .NET Aspire orchestration.

> **Data safety:** local development, deployed validation environments, tests, logs,
> screenshots, issues, and pull requests must use synthetic data. The current environment is
> not approved for patient-linked data. See [Production deployment](docs/production-deployment.md).

## Run in the Dev Container

The Dev Container is the recommended development environment. It provides .NET 10, Node.js
24, Docker-in-Docker, Angular tooling, and Aspire CLI 13.5.2.

Prerequisites on the host:

- Docker Desktop
- Visual Studio Code or another client that supports Dev Containers

Open the repository in its Dev Container, then run from `/workspaces/mdsweep`:

```bash
dotnet tool restore
dotnet restore Mdsweep.slnx
npm ci --prefix src/Mdsweep.Web
aspire run
```

The first run downloads PostgreSQL and Keycloak images and can take several minutes. Aspire
starts the application in dependency order and applies the checked-in EF Core migrations to a
fresh local database.

Use `aspire run --detach` to leave the application running after the terminal closes. Inspect or
stop a detached run with:

```bash
aspire ps
aspire stop --apphost src/Mdsweep.AppHost/Mdsweep.AppHost.csproj
```

## Local endpoints

| Resource | URL |
| --- | --- |
| Web application | <http://localhost:4200> |
| API | <http://localhost:5080> |
| API readiness | <http://localhost:5080/health> |
| Keycloak | <http://localhost:8081> |
| Aspire dashboard | Run `aspire ps` for the authenticated dashboard URL |

The synthetic Dispatcher account imported into the local `mdsweep` realm is:

```text
Username: developer@mdsweep.com
Password: P@ssw0rd!
```

The local-only Keycloak bootstrap administrator is `admin` with password `P@ssw0rd!`. These
development credentials are public repository fixtures and must never be reused outside local
synthetic development.

## Build and test

Run the CI-equivalent checks from the repository root:

```bash
dotnet restore Mdsweep.slnx
dotnet build Mdsweep.slnx --configuration Release --no-restore
dotnet test Mdsweep.slnx --configuration Release --no-build

npm ci --prefix src/Mdsweep.Web
npm run build --prefix src/Mdsweep.Web
npm test --prefix src/Mdsweep.Web -- --watch=false
```

The .NET integration tests require Docker because they exercise the public HTTP workflows
against PostgreSQL. Tests and checked-in fixtures use synthetic data.

## Project map

```text
src/
  Mdsweep.Api/             HTTP, authentication, antiforgery, and application startup
  Mdsweep.Application/     Command/query contracts and application results
  Mdsweep.Domain/          Domain state and rules
  Mdsweep.Infrastructure/  EF Core, handlers, file parsing, Keycloak, and clocks
  Mdsweep.AppHost/         Local Aspire composition and Azure publish model
  Mdsweep.ServiceDefaults/ Health checks, telemetry, and service discovery
  Mdsweep.Web/             Angular PWA
tests/
  Mdsweep.Api.IntegrationTests/
```

The API is a backend-for-frontend. ASP.NET Core completes the Keycloak authorization-code flow,
stores the authenticated session in an HttpOnly cookie, resolves the active Provider context,
and enforces application authorization. Angular uses same-origin HTTP endpoints and does not
receive or store Keycloak access or refresh tokens.

## Dev Container troubleshooting

If Git reports every file as modified after opening a Windows bind-mounted checkout, verify that
the diff contains only line-ending changes before changing configuration:

```bash
git diff --ignore-space-at-eol --quiet
echo $?
```

An exit code of `0` means the content is otherwise unchanged. For that Windows-mounted checkout,
set the repository-local conversion mode and check status again:

```bash
git config core.autocrlf true
git status --short
```

If a public image pull fails because the Dev Container credential helper is unavailable, use an
empty task-specific Docker configuration rather than editing or deleting your normal Docker
credentials:

```bash
mkdir -p /tmp/mdsweep-docker-config
DOCKER_CONFIG=/tmp/mdsweep-docker-config aspire run
```

## Architecture and scope

- [Domain language](CONTEXT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [MVP specification](docs/specs/mvp.md)
- [Architecture decisions](docs/adr/)
- [Production deployment](docs/production-deployment.md)
