using FluentValidation;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Invitations.IssuePreviewToken;
using GuestManagementService.Application.Invitations.RevokePublicLink;
using GuestManagementService.Application.Invitations.SetPublicLink;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Invitations;
using Moq;

namespace GuestManagementService.UnitTests.Invitations;

/// <summary>B7: the public event link toggle/revoke, and preview token issuance.</summary>
public sealed class PublicLinkAndPreviewTests
{
    private static readonly Guid EventId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");
    private static readonly Guid TenantId = Guid.Parse("0fa219ed-70ad-4e8d-9f51-6e60409dc659");
    private static readonly Guid OtherTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    // ---------- SetPublicLink ----------

    [Fact]
    public async Task Enable_DefaultsOff_ThenMintsATokenOnFirstEnable()
    {
        var settings = NewSnapshottedSettings();
        Assert.False(settings.PublicLinkEnabled);

        var result = await SetPublicLink(settings, enabled: true);

        Assert.Equal(SetPublicLinkStatus.Updated, result.Status);
        Assert.True(result.Enabled);
        Assert.False(string.IsNullOrEmpty(result.PublicEventToken));
    }

    [Fact]
    public async Task Enable_WithoutASavedTemplate_IsRejected()
    {
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInvitationSettings?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => SetPublicLink(settings, enabled: true));

        Assert.NotEmpty(exception.Errors);
    }

    [Fact]
    public async Task Disable_TurnsItOffButKeepsAnyExistingToken()
    {
        var settings = NewSnapshottedSettings();
        settings.EnablePublicLink(() => "existing-token", Now);

        var result = await SetPublicLink(settings, enabled: false);

        Assert.False(result.Enabled);
        Assert.Equal("existing-token", result.PublicEventToken);
    }

    [Fact]
    public async Task Enable_ForAnotherTenantsEvent_ReturnsNotFound()
    {
        var settings = NewSnapshottedSettings();

        var result = await SetPublicLink(settings, enabled: true, callerTenantId: OtherTenantId);

        Assert.Equal(SetPublicLinkStatus.EventNotFound, result.Status);
    }

    // ---------- RevokePublicLink ----------

    [Fact]
    public async Task Revoke_RotatesTheTokenSoThePreviousUrlStopsResolving()
    {
        var settings = NewSnapshottedSettings();
        settings.EnablePublicLink(() => "original-token", Now);

        var result = await Revoke(settings);

        Assert.Equal(RevokePublicLinkStatus.Revoked, result.Status);
        Assert.NotEqual("original-token", result.PublicEventToken);
        Assert.NotEqual("original-token", settings.PublicEventToken);
    }

    [Fact]
    public async Task Revoke_WhenNeverEnabled_IsRejected()
    {
        var settings = NewSnapshottedSettings();

        await Assert.ThrowsAsync<ValidationException>(() => Revoke(settings));
    }

    [Fact]
    public async Task Revoke_ForAnotherTenantsEvent_ReturnsNotFound()
    {
        var settings = NewSnapshottedSettings();
        settings.EnablePublicLink(() => "original-token", Now);

        var result = await Revoke(settings, callerTenantId: OtherTenantId);

        Assert.Equal(RevokePublicLinkStatus.EventNotFound, result.Status);
    }

    // ---------- IssuePreviewToken ----------

    [Fact]
    public async Task Issue_MintsATokenExpiringInTheConfiguredLifetime()
    {
        var settings = NewSnapshottedSettings();

        var result = await IssuePreview(settings);

        Assert.Equal(IssuePreviewTokenStatus.Issued, result.Status);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal(Now.Add(IssuePreviewTokenCommandHandler.PreviewTokenLifetime), result.ExpiresAt);
    }

    [Fact]
    public async Task Issue_ReIssuingOverwritesThePreviousToken()
    {
        var settings = NewSnapshottedSettings();
        settings.IssuePreviewToken("first-token", Now.AddMinutes(15), Now);

        var result = await IssuePreview(settings);

        Assert.NotEqual("first-token", result.Token);
        Assert.Equal(result.Token, settings.PreviewToken);
    }

    [Fact]
    public async Task Issue_WithoutASavedTemplate_IsRejected()
    {
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInvitationSettings?)null);

        await Assert.ThrowsAsync<ValidationException>(() => IssuePreview(settings));
    }

    [Fact]
    public async Task Issue_ForAnotherTenantsEvent_ReturnsNotFound()
    {
        var settings = NewSnapshottedSettings();

        var result = await IssuePreview(settings, callerTenantId: OtherTenantId);

        Assert.Equal(IssuePreviewTokenStatus.EventNotFound, result.Status);
    }

    // ---------- helpers ----------

    private static EventInvitationSettings NewSnapshottedSettings() =>
        EventInvitationSettings.Create(
            EventId, TenantId, "{}", TemplateId, 1, "<html><body></body></html>", null, null, Now);

    private static async Task<SetPublicLinkResult> SetPublicLink(
        EventInvitationSettings settings, bool enabled, Guid? callerTenantId = null) =>
        await SetPublicLink(Repository(settings), enabled, callerTenantId);

    private static async Task<SetPublicLinkResult> SetPublicLink(
        Mock<IEventInvitationSettingsRepository> settings, bool enabled, Guid? callerTenantId = null)
    {
        var handler = new SetPublicLinkCommandHandler(
            Events(),
            settings.Object,
            TokenGenerator(),
            Mock.Of<IUnitOfWork>(),
            TimeProvider(Now));

        return await handler.Handle(
            new SetPublicLinkCommand(EventId, enabled)
            {
                CurrentUser = new CurrentUser(Guid.NewGuid(), callerTenantId ?? TenantId),
            },
            CancellationToken.None);
    }

    private static async Task<RevokePublicLinkResult> Revoke(
        EventInvitationSettings settings, Guid? callerTenantId = null)
    {
        var handler = new RevokePublicLinkCommandHandler(
            Events(),
            Repository(settings).Object,
            TokenGenerator(),
            Mock.Of<IUnitOfWork>(),
            TimeProvider(Now));

        return await handler.Handle(
            new RevokePublicLinkCommand(EventId)
            {
                CurrentUser = new CurrentUser(Guid.NewGuid(), callerTenantId ?? TenantId),
            },
            CancellationToken.None);
    }

    private static async Task<IssuePreviewTokenResult> IssuePreview(
        EventInvitationSettings settings, Guid? callerTenantId = null) =>
        await IssuePreview(Repository(settings), callerTenantId);

    private static async Task<IssuePreviewTokenResult> IssuePreview(
        Mock<IEventInvitationSettingsRepository> settings, Guid? callerTenantId = null)
    {
        var handler = new IssuePreviewTokenCommandHandler(
            Events(),
            settings.Object,
            TokenGenerator(),
            Mock.Of<IUnitOfWork>(),
            TimeProvider(Now));

        return await handler.Handle(
            new IssuePreviewTokenCommand(EventId)
            {
                CurrentUser = new CurrentUser(Guid.NewGuid(), callerTenantId ?? TenantId),
            },
            CancellationToken.None);
    }

    private static Mock<IEventInvitationSettingsRepository> Repository(EventInvitationSettings settings)
    {
        var repository = new Mock<IEventInvitationSettingsRepository>();
        repository
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        return repository;
    }

    private static IInvitationTokenGenerator TokenGenerator()
    {
        var generator = new Mock<IInvitationTokenGenerator>();
        generator.Setup(g => g.Generate()).Returns(() => Guid.NewGuid().ToString("N"));

        return generator.Object;
    }

    private static TimeProvider TimeProvider(DateTimeOffset now)
    {
        var provider = new Mock<TimeProvider>();
        provider.Setup(p => p.GetUtcNow()).Returns(now);

        return provider.Object;
    }

    private static IEventReferenceRepository Events()
    {
        var events = new Mock<IEventReferenceRepository>();
        events
            .Setup(r => r.GetByIdAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(
                EventId,
                "Ada's wedding",
                TenantId,
                Now,
                "wedding",
                new DateOnly(2026, 9, 12),
                timeZoneId: "UTC"));

        return events.Object;
    }
}
