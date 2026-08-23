using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Invitations.RenderInvitation;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using GuestManagementService.Domain.Invitations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Invitations;

/// <summary>
/// Table-driven coverage of the render-token matrix. Previewing moved to
/// template-management-service, so this handler resolves only two token types: a guest's own
/// invitation token, and the event's public link token. Every failure — unknown or revoked —
/// returns an identically-shaped 404.
/// </summary>
public sealed class ResolveInvitationRenderQueryHandlerTests
{
    private static readonly Guid EventId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");
    private static readonly Guid TenantId = Guid.Parse("0fa219ed-70ad-4e8d-9f51-6e60409dc659");
    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    private const string GuestToken = "guest-token-123";
    private const string PublicToken = "public-token-456";
    private const string UnknownToken = "unknown-token-000";

    // ---------- guest token ----------

    [Fact]
    public async Task GuestToken_RendersRealGuestDataWithLiveRsvp()
    {
        var result = await Handle(GuestToken);

        Assert.Equal(ResolveInvitationRenderStatus.Guest, result.Status);
        Assert.Equal("Ada", result.GuestName);
    }

    // ---------- public event token ----------

    [Fact]
    public async Task PublicToken_RendersWithGuestNameUnbound()
    {
        var result = await Handle(PublicToken);

        Assert.Equal(ResolveInvitationRenderStatus.PublicEvent, result.Status);
        Assert.Null(result.GuestName);
    }

    [Fact]
    public async Task PublicToken_WhenLinkIsDisabled_ReturnsNotFound()
    {
        // Disabling must stop the link from resolving at all — not merely hide it in the UI.
        var result = await Handle(PublicToken, publicLinkEnabled: false);

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
    }

    // ---------- uniform 404 across every failure mode ----------

    [Fact]
    public async Task UnknownToken_ReturnsNotFound()
    {
        var result = await Handle(UnknownToken);

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task RevokedPublicToken_ReturnsTheSameNotFoundAsAnUnknownToken()
    {
        // Revocation is modelled as the old token no longer matching any row — the handler cannot
        // distinguish "never existed" from "existed, now revoked", which is exactly the point.
        var result = await Handle("a-since-rotated-token");

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UnknownAndRevokedTokens_ProduceTheIdenticalNotFoundShape()
    {
        var unknown = await Handle(UnknownToken);
        var revoked = await Handle("a-since-rotated-token");

        Assert.Equal(unknown, revoked);
    }

    [Fact]
    public async Task BlankToken_ReturnsNotFoundWithoutQueryingAnything()
    {
        var guests = new Mock<IGuestRepository>();
        var handler = CreateHandler(guests.Object, publicLinkEnabled: true);

        var result = await handler.Handle(new ResolveInvitationRenderQuery(""), CancellationToken.None);

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
        guests.Verify(
            r => r.GetByInvitationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- helpers ----------

    private static async Task<ResolveInvitationRenderResult> Handle(
        string token,
        bool publicLinkEnabled = true,
        string fieldValuesJson = "{}")
    {
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(r => r.GetByInvitationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string t, CancellationToken _) => t == GuestToken ? NewGuest() : null);

        var handler = CreateHandler(guests.Object, publicLinkEnabled, fieldValuesJson);

        return await handler.Handle(new ResolveInvitationRenderQuery(token), CancellationToken.None);
    }

    private static ResolveInvitationRenderQueryHandler CreateHandler(
        IGuestRepository guests,
        bool publicLinkEnabled,
        string fieldValuesJson = "{}")
    {
        var events = new Mock<IEventReferenceRepository>();
        events
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewEvent());

        var settings = new Mock<IEventInvitationSettingsRepository>();

        settings
            .Setup(r => r.GetByEventIdAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewSettings(fieldValuesJson));

        settings
            .Setup(r => r.GetByPublicEventTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string t, CancellationToken _) =>
                t == PublicToken ? NewSettings(fieldValuesJson, publicLinkEnabled, PublicToken) : null);

        return new ResolveInvitationRenderQueryHandler(
            guests,
            events.Object,
            settings.Object,
            NullLogger<ResolveInvitationRenderQueryHandler>.Instance);
    }

    private static EventInvitationSettings NewSettings(
        string fieldValuesJson,
        bool publicLinkEnabled = false,
        string? publicToken = null)
    {
        var settings = EventInvitationSettings.Create(
            EventId, TenantId, fieldValuesJson, TemplateId, "Marigold", 1, "<html><body></body></html>", null, null, Now);

        if (publicToken is not null)
        {
            settings.EnablePublicLink(() => publicToken, Now);

            if (!publicLinkEnabled)
            {
                settings.DisablePublicLink(Now);
            }
        }

        return settings;
    }

    private static Guest NewGuest()
    {
        return Guest.Create(
            Guid.NewGuid(),
            EventId,
            TenantId,
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            null,
            GuestToken,
            Now);
    }

    private static EventReference NewEvent()
    {
        return EventReference.Active(
            EventId,
            "Ada's wedding",
            TenantId,
            Now,
            "wedding",
            new DateOnly(2026, 9, 12),
            new TimeOnly(18, 30),
            "Asia/Ho_Chi_Minh",
            "Rosewood Hall",
            "12 Sample Street",
            null);
    }
}
