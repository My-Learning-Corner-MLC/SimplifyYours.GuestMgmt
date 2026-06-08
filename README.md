# Guest Management Service

Backend service for Simplify Yours guest management capabilities.

## Current API

### `GET /ping`

Returns a Guest Management Service up message and the current GMT/UTC date-time.

Responses:

- `200 OK` with the service-up message and GMT/UTC timestamp.

Response body:

```json
{
  "message": "Guest Management service is up.",
  "currentGmtDateTime": "2026-05-23T08:30:45+00:00"
}
```

## Configuration

No persistence, cache, messaging, or external service configuration is required for the current scaffold.

## Local Observability

Start shared infrastructure before running the API:

```bash
docker compose --env-file ../../infra/shared-infrastructure/infrastructure/.env -f ../../infra/shared-infrastructure/infrastructure/docker-compose.yml up -d --remove-orphans
```

The API launch profiles export logs, traces, and metrics to the local Aspire
Dashboard:

```text
OTEL_SERVICE_NAME=guest-management-service
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_HEADERS=x-otlp-api-key=<SIMPLIFYYOURS_ASPIRE_OTLP_API_KEY>
OTEL_RESOURCE_ATTRIBUTES=service.namespace=SimplifyYours,deployment.environment=local
```

Set `OTEL_EXPORTER_OTLP_HEADERS` in your shell before running the service. The
value must match `SIMPLIFYYOURS_ASPIRE_OTLP_API_KEY` from the shared
infrastructure `infrastructure/.env` file.

Open `http://localhost:18888` and use the token from
`docker container logs simplify-yours-aspire-dashboard`.

Do not log request bodies, response bodies, passwords, tokens, authorization
codes, refresh tokens, payment data, customer data, or unnecessary personal
data. Prefer safe context such as operation name, event ID, correlation ID,
causation ID, status, elapsed time, and attempt count.

## Developer Commands

Run these commands from `code/backend/guest-management-service/`.

### Restore

```bash
dotnet restore GuestManagementService.sln
```

### Build

```bash
dotnet build GuestManagementService.sln --configuration Release --no-restore
```

### Test

```bash
dotnet test GuestManagementService.sln --configuration Release --no-build
```

### Test With Coverage

```bash
dotnet test GuestManagementService.sln --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

### Run The API Locally

```bash
dotnet run --project src/GuestManagementService.Api/GuestManagementService.Api.csproj
```

## README Maintenance

Keep this README up to date during development. When a feature introduces a new endpoint, configuration value, migration workflow, local dependency, test command, script, or operational command, add or update the relevant README section in the same change.

## CI Checks

```bash
dotnet restore GuestManagementService.sln
dotnet build GuestManagementService.sln --configuration Release --no-restore
dotnet test GuestManagementService.sln --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```
