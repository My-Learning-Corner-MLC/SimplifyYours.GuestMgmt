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

## Developer Commands

Run these commands from `code/backend/guest-management-service/SimplifyYours.GuestMgmt/`.

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
