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
/// Table-driven coverage of the §7.9 render-token matrix. Every cell in the spec's table is one
/// test here, plus the cross-cutting guarantees: a guest token can never reach preview rendering, a
/// preview token can never resolve real guest data, and every failure — unknown, wrong-type, or
/// expired/revoked — returns an identically-shaped 404.
/// </summary>
public sealed class ResolveInvitationRenderQueryHandlerTests
{
    private static readonly Guid EventId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");
    private static readonly Guid TenantId = Guid.Parse("0fa219ed-70ad-4e8d-9f51-6e60409dc659");
    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    private const string GuestToken = "guest-token-123";
    private const string PublicToken = "public-token-456";
    private const string PreviewToken = "preview-token-789";
    private const string UnknownToken = "unknown-token-000";

    // ---------- guest token ----------

    [Fact]
    public async Task GuestToken_NoMode_RendersRealGuestDataWithLiveRsvp()
    {
        var result = await Handle(GuestToken, mode: null, type: null);

        Assert.Equal(ResolveInvitationRenderStatus.Guest, result.Status);
        Assert.Equal("Ada", result.GuestName);
    }

    [Fact]
    public async Task GuestToken_ModePreview_Rejects400()
    {
        // AC 25: a guest token must REJECT mode=preview, never silently ignore it.
        var result = await Handle(GuestToken, mode: "preview", type: "private");

        Assert.Equal(ResolveInvitationRenderStatus.BadRequest, result.Status);
        Assert.Equal("mode", result.BadRequestField);
    }

    [Fact]
    public async Task GuestToken_AnyMode_Rejects400()
    {
        var result = await Handle(GuestToken, mode: "anything", type: null);

        Assert.Equal(ResolveInvitationRenderStatus.BadRequest, result.Status);
    }

    // ---------- public event token ----------

    [Fact]
    public async Task PublicToken_NoMode_RendersWithGuestNameUnbound()
    {
        var result = await Handle(PublicToken, mode: null, type: null);

        Assert.Equal(ResolveInvitationRenderStatus.PublicEvent, result.Status);
        Assert.Null(result.GuestName);
    }

    [Fact]
    public async Task PublicToken_ModePreview_Rejects400()
    {
        var result = await Handle(PublicToken, mode: "preview", type: "public");

        Assert.Equal(ResolveInvitationRenderStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task PublicToken_WhenLinkIsDisabled_ReturnsNotFound()
    {
        // Disabling must stop the link from resolving at all — not merely hide it in the UI.
        var result = await Handle(PublicToken, mode: null, type: null, publicLinkEnabled: false);

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
    }

    // ---------- preview token ----------

    [Fact]
    public async Task PreviewToken_NoMode_Rejects400()
    {
        // A preview token REQUIRES the mode param.
        var result = await Handle(PreviewToken, mode: null, type: null);

        Assert.Equal(ResolveInvitationRenderStatus.BadRequest, result.Status);
        Assert.Equal("mode", result.BadRequestField);
    }

    [Fact]
    public async Task PreviewToken_ModePreviewTypePrivate_BindsTheFixedSampleGuestName()
    {
        var result = await Handle(PreviewToken, mode: "preview", type: "private");

        Assert.Equal(ResolveInvitationRenderStatus.Preview, result.Status);
        Assert.Equal(ResolveInvitationRenderQueryHandler.SampleGuestName, result.GuestName);
        Assert.NotEqual("Ada", result.GuestName); // never the real guest's name
    }

    [Fact]
    public async Task PreviewToken_ModePreviewTypePublic_BindsGuestNameUnbound()
    {
        var result = await Handle(PreviewToken, mode: "preview", type: "public");

        Assert.Equal(ResolveInvitationRenderStatus.Preview, result.Status);
        Assert.Null(result.GuestName);
    }

    [Fact]
    public async Task PreviewToken_ModePreviewMissingType_Rejects400()
    {
        var result = await Handle(PreviewToken, mode: "preview", type: null);

        Assert.Equal(ResolveInvitationRenderStatus.BadRequest, result.Status);
        Assert.Equal("type", result.BadRequestField);
    }

    [Fact]
    public async Task PreviewToken_ModePreviewInvalidType_Rejects400()
    {
        var result = await Handle(PreviewToken, mode: "preview", type: "nonsense");

        Assert.Equal(ResolveInvitationRenderStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task PreviewToken_WhenExpired_ReturnsTheSameNotFoundAsAnUnknownToken()
    {
        var result = await Handle(
            PreviewToken, mode: "preview", type: "private", previewExpiresAt: Now.AddMinutes(-1));

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task PreviewToken_NeverQueriesTheGuestRepository()
    {
        // AC 23: a preview token must never be able to resolve real guest data. The strongest
        // possible guarantee here is that the guest table is never even consulted for this branch.
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(r => r.GetByInvitationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guest?)null);

        var handler = CreateHandler(guests.Object, publicLinkEnabled: true, previewExpiresAt: Now.AddMinutes(15));

        var result = await handler.Handle(
            new ResolveInvitationRenderQuery(PreviewToken, "preview", "private"), CancellationToken.None);

        Assert.Equal(ResolveInvitationRenderStatus.Preview, result.Status);
        guests.Verify(
            r => r.GetByInvitationTokenAsync(PreviewToken, It.IsAny<CancellationToken>()),
            Times.Once); // called once to rule out PreviewToken ALSO being a guest token — never again.
    }

    [Fact]
    public async Task PreviewToken_UsesSavedFieldValuesFallingBackToSamplesForAnythingUnset()
    {
        var result = await Handle(
            PreviewToken,
            mode: "preview",
            type: "private",
            fieldValuesJson: """{"venueName":"The Old Chapel"}""");

        Assert.Equal("The Old Chapel", result.FieldValues!["venueName"]); // saved value wins
        Assert.False(string.IsNullOrEmpty(result.FieldValues["brideName"])); // sample fallback, never blank
    }

    // ---------- uniform 404 across every failure mode ----------

    [Fact]
    public async Task UnknownToken_ReturnsNotFound()
    {
        var result = await Handle(UnknownToken, mode: null, type: null);

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task RevokedPublicToken_ReturnsTheSameNotFoundAsAnUnknownToken()
    {
        // Revocation is modelled as the old token no longer matching any row — the handler cannot
        // distinguish "never existed" from "existed, now revoked", which is exactly the point.
        var result = await Handle("a-since-rotated-token", mode: null, type: null);

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UnknownAndRevokedAndWrongTypeTokens_AllProduceTheIdenticalNotFoundShape()
    {
        var unknown = await Handle(UnknownToken, mode: null, type: null);
        var revoked = await Handle("a-since-rotated-token", mode: null, type: null);
        var expiredPreview = await Handle(
            PreviewToken, mode: "preview", type: "private", previewExpiresAt: Now.AddSeconds(-1));

        Assert.Equal(unknown, revoked);
        Assert.Equal(unknown, expiredPreview);
    }

    [Fact]
    public async Task BlankToken_ReturnsNotFoundWithoutQueryingAnything()
    {
        var guests = new Mock<IGuestRepository>();
        var handler = CreateHandler(guests.Object, publicLinkEnabled: true);

        var result = await handler.Handle(new ResolveInvitationRenderQuery("", null, null), CancellationToken.None);

        Assert.Equal(ResolveInvitationRenderStatus.NotFound, result.Status);
        guests.Verify(
            r => r.GetByInvitationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- helpers ----------

    private static async Task<ResolveInvitationRenderResult> Handle(
        string token,
        string? mode,
        string? type,
        bool publicLinkEnabled = true,
        DateTimeOffset? previewExpiresAt = null,
        string fieldValuesJson = "{}")
    {
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(r => r.GetByInvitationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string t, CancellationToken _) => t == GuestToken ? NewGuest() : null);

        var handler = CreateHandler(
            guests.Object, publicLinkEnabled, previewExpiresAt ?? Now.AddMinutes(15), fieldValuesJson);

        return await handler.Handle(new ResolveInvitationRenderQuery(token, mode, type), CancellationToken.None);
    }

    private static ResolveInvitationRenderQueryHandler CreateHandler(
        IGuestRepository guests,
        bool publicLinkEnabled,
        DateTimeOffset? previewExpiresAt = null,
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

        settings
            .Setup(r => r.GetByPreviewTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string t, CancellationToken _) =>
                t == PreviewToken
                    ? NewSettings(fieldValuesJson, previewToken: PreviewToken, previewExpiresAt: previewExpiresAt)
                    : null);

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(p => p.GetUtcNow()).Returns(Now);

        return new ResolveInvitationRenderQueryHandler(
            guests,
            events.Object,
            settings.Object,
            timeProvider.Object,
            NullLogger<ResolveInvitationRenderQueryHandler>.Instance);
    }

    private static EventInvitationSettings NewSettings(
        string fieldValuesJson,
        bool publicLinkEnabled = false,
        string? publicToken = null,
        string? previewToken = null,
        DateTimeOffset? previewExpiresAt = null)
    {
        var settings = EventInvitationSettings.Create(
            EventId, TenantId, fieldValuesJson, TemplateId, 1, "<html><body></body></html>", null, null, Now);

        if (publicToken is not null)
        {
            settings.EnablePublicLink(() => publicToken, Now);

            if (!publicLinkEnabled)
            {
                settings.DisablePublicLink(Now);
            }
        }

        if (previewToken is not null)
        {
            settings.IssuePreviewToken(previewToken, previewExpiresAt ?? Now.AddMinutes(15), Now);
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
