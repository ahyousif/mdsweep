# Production deployment

Production deploys run from `.github/workflows/ci-deploy.yml` after verification succeeds on `main`. The workflow uses Aspire 13.5.3 and Azure workload identity federation; it does not use an Azure client secret.

The current Azure target is:

- subscription configured by the `AZURE_SUBSCRIPTION_ID` GitHub Environment variable
- resource group `rg-mdsweep-prod`
- region `westus3`
- Azure Container Apps `api` and `keycloak`
- PostgreSQL flexible server `postgres-67bmnogbc2h74`
- shared Key Vault `mdsweepkv-67bmnogbc2h74`

The environment remains synthetic-only until the deployment-readiness issue defines and verifies database migration, backup, restore, Keycloak administration, and data-safety approval.

## Provision a tenant

Production API startup does not create the application database or run EF Core migrations. The manually triggered `utility` Azure Container Apps Job owns those operations: `tenant provision` ensures that only the `mdsweep` database exists, applies all checked-in migrations, creates the Tenant and its Administrator invitation, and sends the normal invitation email. The independent `keycloak` database is never created, dropped, migrated, or otherwise modified by this workflow.

The job uses its system-assigned managed identity for Azure Resource Manager database operations and the Key Vault-backed PostgreSQL connection supplied by Aspire for EF migrations and application writes. Give the job identity `Contributor` only at the PostgreSQL flexible-server resource scope; do not grant it resource-group-wide access:

```bash
UTILITY_PRINCIPAL_ID="$(az containerapp job show \
  --name <actual-deployed-utility-job-name> \
  --resource-group rg-mdsweep-prod \
  --query identity.principalId \
  --output tsv)"
POSTGRES_SCOPE="$(az postgres flexible-server show \
  --name postgres-67bmnogbc2h74 \
  --resource-group rg-mdsweep-prod \
  --query id \
  --output tsv)"

az role assignment create \
  --assignee-object-id "$UTILITY_PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "$POSTGRES_SCOPE"
```

Discover the deployed job name first (Aspire may append a generated suffix):

```bash
az containerapp job list \
  --resource-group rg-mdsweep-prod \
  --query "[?contains(name, 'utility')].name" \
  --output table
```

Provision a Tenant with one execution:

```bash
az containerapp job start \
  --name <actual-deployed-utility-job-name> \
  --resource-group rg-mdsweep-prod \
  --args \
    tenant provision \
    --tenant-id abcd-efgh-1234 \
    --tenant-name "Acme Transportation" \
    --admin-email "owner@acme.com" \
    --admin-first-name "John" \
    --admin-last-name "Doe"
```

`--admin-display-name "John Doe"` is optional; invitation acceptance otherwise uses the administrator's first and last names. The command-line arguments contain no database credentials, Keycloak administrator credentials, or Keycloak subject. Aspire's `WithReference(database)` supplies `ConnectionStrings__mdsweep`; the job has no reference to `keycloak-db`.

The job definition uses the Azure manual trigger type and the local Aspire resource uses explicit start, so neither `aspire deploy` nor `aspire run` executes it. A compatible rerun reports that no work is required. A conflicting Tenant name or failed persistence operation returns a non-zero exit code without retaining partial Tenant or invitation state.

If it fails, list executions and inspect the selected execution's console logs in the Azure portal (Container Apps Job > Execution history > Console logs):

```bash
az containerapp job execution list \
  --name <actual-deployed-utility-job-name> \
  --resource-group rg-mdsweep-prod \
  --output table
```

The recipient follows the normal invitation link, signs in to an existing Keycloak account or registers through Keycloak, and accepts the invitation. Only then does MDSweep create or reuse the local User and create the Administrator Tenant Membership.

The utility also exposes `database migrate`, which safely ensures and migrates `mdsweep`. To destroy and recreate only the `mdsweep` database in the current synthetic/pre-production environment, use the explicit confirmation below. Never run this after approval for patient-linked data:

```bash
az containerapp job start \
  --name <actual-deployed-utility-job-name> \
  --resource-group rg-mdsweep-prod \
  --args \
    database reset \
    --confirm mdsweep
```

Reset never targets the `keycloak` database.

## Reset the synthetic demo environment

`demo reset --confirm RESET-DEMO` is a separate, destructive, synthetic-only operation. It uses the permanent Keycloak automation service account in `master` to delete and recreate only the `mdsweep` realm, recreates `mdsweep-api` with the existing `OIDC_CLIENT_SECRET`, and creates `demo.admin@mdsweep.test`. It then resets and migrates only the `mdsweep` application database and creates the MDSweep Demo Tenant, local User, and active Administrator Tenant Membership linked to that new Keycloak user. It never drops or resets `keycloak-db`.

Run it only with the manually dispatched **Reset Demo Environment** GitHub Actions workflow. The workflow requires the literal `RESET-DEMO` input, uses the protected `production` environment, logs in through the existing GitHub OIDC identity, discovers the deployed Utility job, and starts:

```text
demo reset --confirm RESET-DEMO
```

The reset is intentionally not part of `ci-deploy.yml`, so an ordinary `main` deployment cannot invoke it. The demo administrator password is a production-environment secret and is never printed by the Utility or workflow. Treat all resulting data as synthetic.

## One-time GitHub OIDC setup

Create a user-assigned identity and federate only the protected GitHub `production` environment:

```bash
az identity create --name mdsweep-github-deploy --resource-group rg-mdsweep-prod --location westus3

az identity federated-credential create \
  --name github-production \
  --identity-name mdsweep-github-deploy \
  --resource-group rg-mdsweep-prod \
  --issuer https://token.actions.githubusercontent.com \
  --subject repo:ahyousif@112917886/mdsweep@1330451816:environment:production\
  --audiences api://AzureADTokenExchange
```

Assign these minimum built-in roles at the resource-group scope:

- `Contributor`, to create and update the Aspire-managed resources and deployments.
- `Role Based Access Control Administrator`, to create the narrowly scoped managed-identity role assignments generated by Aspire. This role does not grant resource management itself.

```bash
DEPLOY_PRINCIPAL_ID="$(az identity show --name mdsweep-github-deploy --resource-group rg-mdsweep-prod --query principalId --output tsv)"
RESOURCE_GROUP_ID="$(az group show --name rg-mdsweep-prod --query id --output tsv)"

az role assignment create \
  --assignee-object-id "$DEPLOY_PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "$RESOURCE_GROUP_ID"

az role assignment create \
  --assignee-object-id "$DEPLOY_PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "Role Based Access Control Administrator" \
  --scope "$RESOURCE_GROUP_ID"
```

Create a GitHub Environment named `production`, restrict it to `main`, and add required reviewers. Add these environment variables:

- `AZURE_CLIENT_ID`: client ID of `mdsweep-github-deploy`
- `AZURE_TENANT_ID`: Azure tenant ID
- `AZURE_SUBSCRIPTION_ID`: target subscription ID
- `AZURE_RESOURCE_GROUP`: `rg-mdsweep-prod`
- `AZURE_LOCATION`: `westus3`
- `WEB_BASE_URL`: canonical public origin of the deployed application, such as `https://app.mdsweep.com`
- `SMTP_FROM`: mailbox used as the sender for MDSweep invitation email

Add these environment secrets using the existing production values; do not generate replacements during CI setup:

- `POSTGRES_USERNAME`
- `POSTGRES_PASSWORD`
- `OIDC_CLIENT_SECRET`
- `GOOGLE_ROUTES_API_KEY`
- `SMTP_CONNECTION_STRING`: SMTP endpoint in the form `Endpoint=smtp://host:port`
- `SMTP_USERNAME`
- `SMTP_PASSWORD`
- `KEYCLOAK_AUTOMATION_CLIENT_SECRET`: secret for the permanent `master`-realm `mdsweep-demo-automation` service-account client
- `DEMO_ADMIN_PASSWORD`: password assigned to the synthetic `demo.admin@mdsweep.test` user whenever the demo environment is reset

The AppHost injects the public origin as `Web__BaseUrl` and configures the production SMTP connection as the API and utility job's named `smtp` connection string. Production invitation delivery uses STARTTLS. Keep these settings synthetic until deployment readiness is accepted, and verify the selected SMTP provider is covered by the required agreement before any patient-linked use.

## Permanent Keycloak administrator

Create the administrator with Keycloak's Admin Console rather than adding credentials to the AppHost or realm import:

1. Open the production Keycloak Admin Console and sign in to the `master` realm with the current bootstrap administrator.
2. Create a named, non-shared user with a verified administrator email.
3. Set a strong permanent password through the console's credential flow. Store it in the organization's approved password manager, not GitHub or this repository.
4. Assign the `realm-management` client role `realm-admin` to the user in the `master` realm.
5. Sign out, open a private browser session, sign in as the new user, and confirm that the `mdsweep` realm can be administered.
6. Record the administrator owner and recovery procedure in the deployment-readiness issue.

The production AppHost intentionally does not set `KC_BOOTSTRAP_ADMIN_USERNAME` or `KC_BOOTSTRAP_ADMIN_PASSWORD`. Local `aspire run` still creates the synthetic `admin` bootstrap user and imports the development realm. If production must be recovered onto a fresh database, use Keycloak's native `bootstrap-admin user` recovery command during a controlled maintenance window, then remove that temporary recovery account after a named administrator is restored.

## One-time Keycloak demo-reset automation setup

The demo reset job authenticates with client credentials, not a shared human administrator. In the `master` realm, create a confidential OIDC client named `mdsweep-demo-automation`, enable **Client authentication** and **Service accounts roles**, and retain its generated client secret in the `KEYCLOAK_AUTOMATION_CLIENT_SECRET` production-environment secret. On that client's service-account user, assign the `realm-management` client role `realm-admin` in `master`. This is required to delete and create the `mdsweep` realm; it does not require or use Keycloak database credentials.

Verify the client can obtain a token from `/realms/master/protocol/openid-connect/token` with `grant_type=client_credentials` before enabling the workflow. Restrict access to the `production` GitHub environment, rotate the client secret through the environment secret when needed, and record its owner and recovery procedure in the deployment-readiness issue.

The production `mdsweep-api` client is administered in Keycloak rather than imported by the AppHost. Its Valid Post Logout Redirect URIs must include the deployed public application's `/signout-callback-oidc` URI before enabling RP-initiated logout.
The production `mdsweep` realm must also have **User registration** enabled in Realm settings; the development realm import does not configure the production database.

## Deployment security notes

- Generated `aspire-output` and local `.aspire` deployment state are ignored. Deployment state can contain plain-text parameter values and must never be uploaded as an artifact or committed.
- The GitHub workflow supplies the existing database, OIDC client, Google Routes, public-origin, and SMTP configuration explicitly, so a clean runner does not rotate credentials or depend on a sensitive deployment-state cache.
- Container Apps resolve PostgreSQL connection strings from the shared Key Vault through managed identities. Other Aspire-generated Container App secrets remain platform-managed.
- Reassess whether a custom Azure role can replace `Role Based Access Control Administrator` after the generated role-assignment set stabilizes.
