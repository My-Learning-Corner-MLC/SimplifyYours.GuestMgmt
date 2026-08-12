using System.Text.Json;
using GuestManagementService.Api.Endpoints;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Invitations.GetInvitation;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Invitations;

public sealed class GetInvitationQueryHandlerTests
{
    private const string Token = "tok-abc123";
    private static readonly Guid EventId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WhenTokenResolves_ReturnsGuestAndEvent()
    {
        var result = await Handle(NewGuest(), NewEvent());

        Assert.Equal(GetInvitationStatus.Found, result.Status);
        Assert.Equal("Ada", result.Guest!.FirstName);
        Assert.Equal("Eleanor & Sam", result.Event!.EventName);
        Assert.True(result.IsOpen);
        Assert.Equal(new DateTimeOffset(2026, 9, 12, 16, 59, 59, TimeSpan.Zero), result.Deadline);
    }

    [Fact]
    public async Task Handle_WhenTokenIsUnknown_ReturnsNotFound()
    {
        var result = await Handle(guest: null, NewEvent());

        Assert.Equal(GetInvitationStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenTokenIsBlank_ReturnsNotFoundWithoutQuerying(string token)
    {
        var guests = new Mock<IGuestRepository>();

        var result = await CreateHandler(guests.Object, NewEvent()).Handle(
            new GetInvitationQuery(token),
            CancellationToken.None);

        Assert.Equal(GetInvitationStatus.NotFound, result.Status);
        guests.Verify(
            r => r.GetByInvitationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEventIsDeleted_ReturnsNotFound()
    {
        // A cancelled event must not keep rendering an invitation to it — and the caller must not
        // be able to tell "cancelled" apart from "never existed".
        var deleted = NewEvent();
        deleted.MarkDeleted(Now);

        var result = await Handle(NewGuest(), deleted);

        Assert.Equal(GetInvitationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenEventReferenceIsMissing_ReturnsNotFound()
    {
        var result = await Handle(NewGuest(), eventReference: null);

        Assert.Equal(GetInvitationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_AfterTheDeadline_ReportsRsvpClosed()
    {
        var result = await Handle(NewGuest(), NewEvent(), now: new DateTimeOffset(2026, 9, 13, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(GetInvitationStatus.Found, result.Status);
        Assert.False(result.IsOpen);
    }

    /// <summary>The organiser's saved invitation content, as the endpoint reads it.</summary>
    private static Dictionary<string, string?> Content() => new(StringComparer.Ordinal)
    {
        ["brideName"] = "Amara",
        ["groomName"] = "Julian",
        ["eventDate"] = "Saturday, September 12, 2026",
        ["eventTime"] = "4:00 PM",
        ["venueName"] = "Rosewood Hall",
        ["venueAddress"] = "12 Sample Street",
        ["venueNotes"] = null,
    };

    [Fact]
    public void Response_ExposesOnlyWhatThePageRenders()
    {
        // The single most important assertion in this slice: whoever holds the link can read this
        // without authenticating, so it is checked field-by-field against the serialized payload
        // rather than by inspecting a few properties.
        var response = InvitationEndpoints.ToResponse(
            NewGuest(),
            Content(),
            new DateTimeOffset(2026, 9, 12, 16, 59, 59, TimeSpan.Zero),
            isOpen: true);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.DoesNotContain("ada@example.com", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("+15551234567", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Lovelace", json, StringComparison.Ordinal);   // surname not needed to greet
        Assert.DoesNotContain(EventId.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Token, json, StringComparison.Ordinal);

        Assert.Contains("Ada", json, StringComparison.Ordinal);
        Assert.Contains("Rosewood Hall", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Response_ReadsPlusOnesAllowanceAndGuestAnswersFromMetadata()
    {
        var guest = NewGuest("{\"plusOnes\":2,\"plusOnesConfirmed\":1,\"dietaryNotes\":\"Pescatarian\"}");

        var response = InvitationEndpoints.ToResponse(guest, Content(), null, isOpen: true);

        Assert.Equal(2, response.Rsvp.PlusOnesAllowed);
        Assert.Equal(1, response.Rsvp.PlusOnesConfirmed);
        Assert.Equal("Pescatarian", response.Rsvp.DietaryNotes);
    }

    [Fact]
    public void Response_CarriesTheOrganisersContentRatherThanTheEventRecord()
    {
        // AC 51: an event edited after the invitation was composed must not silently rewrite what
        // guests were shown, so the payload comes from the saved settings.
        var response = InvitationEndpoints.ToResponse(NewGuest(), Content(), null, isOpen: true);

        Assert.Equal("Amara", response.Content["brideName"]);
        Assert.Equal("Rosewood Hall", response.Content["venueName"]);
        Assert.Null(response.Content["venueNotes"]);
    }

    private static async Task<GetInvitationResult> Handle(
        Guest? guest,
        EventReference? eventReference,
        DateTimeOffset? now = null)
    {
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(r => r.GetByInvitationTokenAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guest);

        return await CreateHandler(guests.Object, eventReference, now)
            .Handle(new GetInvitationQuery(Token), CancellationToken.None);
    }

    private static GetInvitationQueryHandler CreateHandler(
        IGuestRepository guests,
        EventReference? eventReference,
        DateTimeOffset? now = null)
    {
        var events = new Mock<IEventReferenceRepository>();
        events
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventReference);

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now ?? Now);

        return new GetInvitationQueryHandler(
            guests,
            events.Object,
            timeProvider.Object,
            NullLogger<GetInvitationQueryHandler>.Instance);
    }

    private static Guest NewGuest(string? metadata = null)
    {
        return Guest.Create(
            Guid.NewGuid(),
            EventId,
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            metadata,
            Token,
            Now);
    }

    private static EventReference NewEvent()
    {
        return EventReference.Active(
            EventId,
            "Eleanor & Sam",
            Guid.NewGuid(),
            Now,
            "wedding",
            new DateOnly(2026, 9, 12),
            new TimeOnly(18, 30),
            "Asia/Ho_Chi_Minh",
            "Rosewood Hall",
            "12 Sample Street",
            null);
    }

    private static EventReference NewEventWithoutVenue()
    {
        return EventReference.Active(EventId, "Eleanor & Sam", Guid.NewGuid(), Now, "wedding");
    }
}
