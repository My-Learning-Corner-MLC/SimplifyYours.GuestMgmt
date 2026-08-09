using System.Text.Json;
using GuestManagementService.Contracts.IntegrationEvents;

namespace GuestManagementService.UnitTests.EventReferences;

public sealed class EventReferencePayloadTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserialize_WhenMessagePredatesDisplayFields_StillBinds()
    {
        // Exactly the shape event-service emits at envelope version 3. Messages in this shape are
        // already on the topic, so the consumer must keep binding them after the contract grows.
        const string json = """
            {
              "eventId": "6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55",
              "eventName": "Launch",
              "tenantId": "0fa219ed-70ad-4e8d-9f51-6e60409dc659",
              "eventType": "wedding"
            }
            """;

        var payload = JsonSerializer.Deserialize<EventReferencePayload>(json, JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal("Launch", payload.EventName);
        Assert.Equal("wedding", payload.EventType);
        Assert.Null(payload.EventDate);
        Assert.Null(payload.TimeZoneId);
        Assert.Null(payload.Location);
    }

    [Fact]
    public void Deserialize_WhenMessageCarriesDisplayFields_BindsThemIncludingLocation()
    {
        const string json = """
            {
              "eventId": "6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55",
              "eventName": "Launch",
              "tenantId": "0fa219ed-70ad-4e8d-9f51-6e60409dc659",
              "eventType": "wedding",
              "eventDate": "2026-09-12",
              "eventStartTime": "18:30:00",
              "eventEndTime": "23:00:00",
              "timeZoneId": "Asia/Ho_Chi_Minh",
              "eventDescription": "An evening reception",
              "location": {
                "venueName": "Rosewood Hall",
                "address": "12 Sample Street",
                "notes": "Parking at the rear"
              }
            }
            """;

        var payload = JsonSerializer.Deserialize<EventReferencePayload>(json, JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal(new DateOnly(2026, 9, 12), payload.EventDate);
        Assert.Equal(new TimeOnly(18, 30), payload.EventStartTime);
        Assert.Equal(new TimeOnly(23, 0), payload.EventEndTime);
        Assert.Equal("Asia/Ho_Chi_Minh", payload.TimeZoneId);
        Assert.Equal("An evening reception", payload.EventDescription);
        Assert.Equal("Rosewood Hall", payload.Location?.VenueName);
        Assert.Equal("12 Sample Street", payload.Location?.Address);
        Assert.Equal("Parking at the rear", payload.Location?.Notes);
    }
}
