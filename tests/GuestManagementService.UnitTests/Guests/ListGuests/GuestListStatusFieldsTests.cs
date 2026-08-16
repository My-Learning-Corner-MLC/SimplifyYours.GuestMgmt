using System.Text.Json;
using GuestManagementService.Application.Guests;
using GuestManagementService.Application.Guests.Wedding;
using GuestManagementService.Domain.Guests;

namespace GuestManagementService.UnitTests.Guests.ListGuests;

public sealed class GuestListStatusFieldsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void From_ProjectsBothStatusAxesAndTheGuestsConfirmedCount()
    {
        var guest = NewGuest("{\"plusOnes\":2,\"plusOnesConfirmed\":1}");
        guest.RecordRsvp(RsvpStatus.Accepted, guest.Metadata, Now);

        var details = GuestDetails.From(guest, Mapper());

        Assert.Equal("NotSent", details.DeliveryStatus);
        Assert.Equal("Accepted", details.RsvpStatus);
        Assert.Equal(Now, details.RespondedAt);
        Assert.Equal(1, details.PlusOnesConfirmed);
    }

    [Fact]
    public void From_DefaultsToNotSentAndNoResponseForANewGuest()
    {
        var details = GuestDetails.From(NewGuest(null), Mapper());

        Assert.Equal("NotSent", details.DeliveryStatus);
        Assert.Equal("NoResponse", details.RsvpStatus);
        Assert.Null(details.RespondedAt);
        Assert.Null(details.PlusOnesConfirmed);
    }

    [Fact]
    public void Serialized_NeverCarriesTheInvitationToken()
    {
        // The token is credential-like: whoever holds it can read a guest's personal data without
        // authenticating. It is served only by GET /guests/{id}/invitation-link, one guest at a
        // time — never in a paginated list that gets cached and logged.
        var details = GuestDetails.From(NewGuest(null), Mapper());

        var json = JsonSerializer.Serialize(details, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.DoesNotContain("tok-", json, StringComparison.Ordinal);
        Assert.DoesNotContain("invitationToken", json, StringComparison.OrdinalIgnoreCase);
    }

    private static IGuestMetadataMapper Mapper() =>
        new WeddingGuestMetadataMapper(new WeddingGuestMetadataRequestValidator());

    private static Guest NewGuest(string? metadata) =>
        Guest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            metadata,
            "tok-abc123",
            Now);
}
