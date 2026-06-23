# Guest Management Service

Backend service for Simplify Yours guest management capabilities.

## Current API

Protected guest resource endpoints require `Authorization: Bearer <access_token>`.
Access tokens must be issued by Identity Service for audience
`simplify-yours-api`. Normal users can add guests only to their own active
event references. `SuperAdmin` can add guests to any active event reference.

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

### `POST /guest`

Adds one guest to an existing event. The service checks the local event reference table, which is synchronized from Event Service integration events, before creating the guest.

Request body:

```json
{
  "eventId": "00000000-0000-0000-0000-000000000000",
  "guestInfo": {
    "firstName": "Ada",
    "lastName": "Lovelace",
    "phoneNumber": "+15551234567",
    "emailAddress": "ada@example.com",
    "gender": "preferNotToSay"
  }
}
```

Request options:

- `eventId`: required.
- `guestInfo`: required.
- `guestInfo.firstName`: required.
- `guestInfo.lastName`: required.
- `guestInfo.phoneNumber`: required.
- `guestInfo.emailAddress`: optional.
- `guestInfo.gender`: optional, one of `male`, `female`, `other`, or `preferNotToSay`; defaults to `preferNotToSay`.

Responses:

- `201 Created` with the created guest details.
- `401 Unauthorized` when the bearer token is missing or invalid.
- `400 Bad Request` with validation details when the request is invalid.
- `404 Not Found` when the event reference does not exist, is deleted, or belongs to another user.
- `409 Conflict` when the same event already has a guest with the same normalized phone number, or the same normalized email address when email is provided.

Response body:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "eventId": "00000000-0000-0000-0000-000000000000",
  "guestInfo": {
    "firstName": "Ada",
    "lastName": "Lovelace",
    "phoneNumber": "+15551234567",
    "emailAddress": "ada@example.com",
    "gender": "preferNotToSay"
  },
  "createdAt": "2026-05-24T00:00:00+00:00"
}
```

## Configuration

The service requires `ConnectionStrings:GuestManagementServiceDb` at runtime. Keep real connection strings out of source control and provide them through environment variables, user secrets, or local-only configuration.

For design-time EF migration commands, set `ConnectionStrings__GuestManagementServiceDb` to a local non-production PostgreSQL connection string.

Protected endpoints also require:

- `Auth:Issuer`: Identity Service issuer URL, for example `https://localhost:15200/`.
- `Auth:Audience`: expected access-token audience, currently `simplify-yours-api`.
- `Auth:AccessTokenEncryptionKeyBase64`: base64-encoded shared access-token encryption key.

Keep real token encryption keys and bearer tokens out of source control.

Guest Management stores local event references synchronized from Event Service `EventCreated`, `EventUpdated`, and `EventDeleted` integration events. Event reference payloads include `ownerUserId` so Guest Management can enforce owner-scoped access without reading the Event Service database. `Kafka:BootstrapServers`, `Kafka:EventReferenceTopic`, and `Kafka:GroupId` configure the Kafka consumer. The consumer is disabled when Kafka configuration is incomplete, and processed messages are tracked in the inbox table for idempotency.

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

### Add A Migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/GuestManagementService.Infrastructure/GuestManagementService.Infrastructure.csproj \
  --startup-project src/GuestManagementService.Api/GuestManagementService.Api.csproj \
  --context GuestManagementServiceDbContext \
  --output-dir Persistence/Migrations
```

### Apply Migrations

```bash
dotnet ef database update \
  --project src/GuestManagementService.Infrastructure/GuestManagementService.Infrastructure.csproj \
  --startup-project src/GuestManagementService.Api/GuestManagementService.Api.csproj \
  --context GuestManagementServiceDbContext
```

### List Migrations

```bash
dotnet ef migrations list \
  --project src/GuestManagementService.Infrastructure/GuestManagementService.Infrastructure.csproj \
  --startup-project src/GuestManagementService.Api/GuestManagementService.Api.csproj \
  --context GuestManagementServiceDbContext
```

## README Maintenance

Keep this README up to date during development. When a feature introduces a new endpoint, configuration value, migration workflow, local dependency, test command, script, or operational command, add or update the relevant README section in the same change.

## CI Checks

```bash
dotnet restore GuestManagementService.sln
dotnet build GuestManagementService.sln --configuration Release --no-restore
dotnet test GuestManagementService.sln --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```
