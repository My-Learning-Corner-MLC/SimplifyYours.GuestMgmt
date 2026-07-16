# Guest Management Service

Backend service for Simplify Yours guest management capabilities.

## Current API

Protected guest resource endpoints require `Authorization: Bearer <access_token>`.
Access tokens must be issued by Identity Service for audience
`simplify-yours-api`. Adding a guest requires the `guests.add` permission and
listing an event's guests requires the `guests.view` permission. Normal users
can add or list guests only for their own active event references; `SuperAdmin`
can do so for any active event reference.

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
    "gender": "preferNotToSay",
    "eventMetadata": {
      "relationship": "Family",
      "side": "Bride",
      "plusOnes": 1,
      "dietaryNotes": "Pescatarian"
    }
  }
}
```

Request options:

- `eventId`: required.
- `guestInfo`: required.
- `guestInfo.firstName`: required.
- `guestInfo.lastName`: required.
- `guestInfo.phoneNumber`: required.
- `guestInfo.emailAddress`: required, valid email.
- `guestInfo.gender`: optional, one of `male`, `female`, `other`, or `preferNotToSay`; defaults to `preferNotToSay`.
- `guestInfo.eventMetadata`: optional object whose shape depends on the event's actual type — see
  below. Omitted/ignored for event types with no registered mapper.

For a **wedding** event (`eventMetadata`):

- `relationship`: optional, one of `Family`, `Friend`, `Colleague`.
- `side`: optional, one of `Bride`, `Groom`.
- `plusOnes`: optional integer `0`–`20`; defaults to `0`.
- `dietaryNotes`: optional, up to 500 characters.

For a **birthday** event (`eventMetadata`):

- `plusOnes`: optional integer `0`–`20`; defaults to `0`.
- `dietaryNotes`: optional, up to 500 characters.

`eventMetadata` is validated against the event's **actual** type, looked up from the local event
reference table (synced from Event Service via Kafka) — the server never assumes a shape. The
resulting value is persisted in a single opaque `metadata` `jsonb` column on the guest, not as
dedicated columns, so each event type can carry its own attributes with no schema change.

Adding a new event type means:

1. A metadata value object under `Guests/<EventType>/` in Domain (e.g. `BirthdayGuestMetadata`).
2. A response contract under `Guests/<EventType>/` in Contracts (e.g. `BirthdayGuestMetadataResponse`).
3. An `IGuestMetadataMapper` implementation under `Guests/<EventType>/` in Application, registered
   in `GuestManagementService.Application.DependencyInjection` — `GuestMetadataMapperFactory`
   discovers it automatically via `IEnumerable<IGuestMetadataMapper>`; no factory changes needed.

Responses:

- `201 Created` with the created guest details.
- `401 Unauthorized` when the bearer token is missing or invalid.
- `400 Bad Request` with validation details when the request is invalid.
- `404 Not Found` when the event reference does not exist, is deleted, or belongs to another user.
- `409 Conflict` when the same event already has a guest with the same normalized phone number or email address.

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
    "gender": "preferNotToSay",
    "eventMetadata": {
      "relationship": "Family",
      "side": "Bride",
      "plusOnes": 1,
      "dietaryNotes": "Pescatarian"
    }
  },
  "createdAt": "2026-05-24T00:00:00+00:00"
}
```

### `POST /guests/query`

Returns a page of one owned event's guests. Requires the `guests.view` permission. The service
enforces owner/tenant scoping via the local event reference table (same rule as `POST /guest`).

Request body:

```json
{
  "eventId": "00000000-0000-0000-0000-000000000000",
  "pageNumber": 1,
  "pageSize": 20,
  "search": "ada",
  "sortBy": "name",
  "sortDirection": "asc"
}
```

- `eventId`: required.
- `pageNumber`: optional, defaults to 1.
- `pageSize`: optional, defaults to 20, max 100.
- `search`: optional, matches first name, last name, or email.
- `sortBy`: optional, one of `name`, `email`, `createdAt` (default).
- `sortDirection`: optional, one of `asc` (default), `desc`.

Responses:

- `200 OK` with a page of guests (empty array when the event has no matching guests).
- `401 Unauthorized` when the bearer token is missing or invalid.
- `404 Not Found` when the event reference does not exist, is deleted, or belongs to another user.

Response body:

```json
{
  "eventId": "00000000-0000-0000-0000-000000000000",
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "firstName": "Ada",
      "lastName": "Lovelace",
      "phoneNumber": "+15551234567",
      "emailAddress": "ada@example.com",
      "gender": "preferNotToSay",
      "eventMetadata": {
        "relationship": "Family",
        "side": "Bride",
        "plusOnes": 1,
        "dietaryNotes": "Pescatarian"
      },
      "createdAt": "2026-05-24T00:00:00+00:00"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
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
