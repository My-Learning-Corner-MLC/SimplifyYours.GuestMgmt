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

## Seating (Table/Seat Layout Builder)

The Seating sub-module lives inside this service (not a separate deployable) and owns an
event's table/seat layout and floor plan. Every event has at most one `SeatingLayout`,
created lazily on first read. All Seating endpoints require `Authorization: Bearer
<access_token>`; reads require `seating.view`, all writes require `seating.manage`.
Every endpoint enforces the same owner/tenant scoping as the guest endpoints via the local
event reference table.

Routing follows the existing convention in this service: `eventId` is a query parameter on
GET/DELETE and a body field on POST/PUT/PATCH — there are no nested `/events/{id}/...` paths.

### Tables

- `GET /seating?eventId={guid}` — loads (or lazily creates) the layout: tables (with a
  `seats` array sized to `seatCount`, each seat carrying `guestId`/`guestName` when occupied
  by a named guest, or `isReservedForParty: true` + `partyOwnerGuestId` when it's an
  anonymous seat reserved for that guest's accompanying attendee — see "Seat assignments"),
  floor-plan areas, and summary counts (`tableCount`, `seatCount`, `seatedCount`,
  `floatingCount`). `seatedCount`/`floatingCount` count named guests only; reserved seats
  aren't a guest and don't affect either.
- `POST /seating/tables` — creates 1–20 tables in one call (`name`, `shape`: `Round`/`Long`/
  `Square`, `seatCount`: 1–20, `count`). When `count > 1`, tables are numbered `name · 1`,
  `name · 2`, ...
- `PUT /seating/tables/{tableId}` — renames/reshapes/resizes a table and toggles "mark full"
  together. Returns `409` if the new `seatCount` would drop below an occupied seat (unseat
  first).
- `DELETE /seating/tables/{tableId}?eventId={guid}` — deletes a table; any guests seated
  there become unseated.
- `PATCH /seating/tables/{tableId}/position` — moves/rotates one table (floor-plan drag,
  single-table fallback).
- `PATCH /seating/tables/positions` — moves/rotates a batch of tables in one call and one
  save; per-table `Applied`/`TableNotFound` status. This is the primary write path for a
  debounced drag flush from the UI — the client should coalesce a drag session into one
  batch call rather than one request per drop.

### Seat assignments ("who sits where")

- `PUT /seating/tables/{tableId}/seats/{seatIndex}` (body: `{ eventId, guestId }`) —
  assigns/moves a guest to that seat. If the guest has accompanying attendees (wedding
  metadata `plusOnes`, today the only source of this count), that many additional
  contiguous adjacent seats are reserved alongside their own — anonymous seats with no
  guest of their own (`guestId: null`, `isReservedForParty: true`), searched by seat index
  outward from the dropped seat (wrapping for Round/Square; Long tables' two rows don't
  wrap into each other). Assigning an already-seated guest relocates their whole party
  (idempotent). Returns `409` if the seat is already occupied/reserved by someone else, or
  if there isn't enough contiguous adjacent room for the whole party (no partial
  assignment — the prior arrangement is left untouched). Kept as the keyboard/click
  accessibility fallback for the drag-and-drop UI.
- `DELETE /seating/tables/{tableId}/seats/{seatIndex}?eventId={guid}` — unseats whoever is
  there, including their whole party if the targeted seat belongs to one; a no-op success
  if the seat is already empty.
- `PUT /seating/assignments` (body: `{ eventId, ops: [{ op: "Assign"|"Unassign", guestId,
  tableId?, seatIndex? }] }`) — the primary write path for the debounced drag-and-drop queue.
  Ops are guest-centric desired-end-state (safe to replay): `Assign` requires `tableId` and
  `seatIndex`, and reserves adjacent party seats the same way the single-seat endpoint
  does; `Unassign` only needs `guestId` and releases their whole party. Applies every op
  against the layout then a single save; returns per-op status (`Applied`/`Conflict`
  covers both seat-occupied and not-enough-adjacent-room/`GuestNotFound`/`TableNotFound`/
  `SeatIndexOutOfRange`) plus the authoritative layout, so one bad op in a batch doesn't
  discard the rest. Capped at 200 ops per call.

### Floor-plan areas

Room elements (stage, dance floor, bar, entrance, buffet, cake) and free-form custom areas
(photo booth, gift table, ...) placed alongside tables on the floor plan.

- `POST /seating/areas` (body: `{ eventId, name, kind, shape, width, height, color?,
  capacity? }`) — `kind`: `Stage`/`DanceFloor`/`Bar`/`Entrance`/`Buffet`/`Cake`/`Custom`;
  `shape`: `Rect`/`Round`/`Free`. Created without a position — the client places it on the
  canvas afterward.
- `PUT /seating/areas/{areaId}` — updates name/kind/shape/size/color/capacity.
- `DELETE /seating/areas/{areaId}?eventId={guid}` — removes an area.
- `PATCH /seating/areas/{areaId}/position` — moves/rotates one area (single fallback).
- `PATCH /seating/areas/positions` — batch move for a debounced drag flush, mirroring the
  table-position batch. Per-area `Applied`/`AreaNotFound` status.

All Seating endpoints return `404` for an unknown/deleted/other-tenant event, table, or area,
and `400` (`problem+json` validation errors) for malformed input. No guest PII beyond a
guest's existing name (already visible via `GET /guests`) is introduced by this sub-module;
seat-assignment logs record IDs only.

## Configuration

The service requires `ConnectionStrings:GuestManagementServiceDb` at runtime. Keep real connection strings out of source control and provide them through environment variables, user secrets, or local-only configuration.

The guest endpoints are called directly from the Angular SPA, so CORS must allow the SPA origin. Configure `Cors:AllowedOrigins` (a string array); it defaults to `http://localhost:4200` for local development.

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
