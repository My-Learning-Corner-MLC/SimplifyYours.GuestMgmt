using FluentValidation;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Invitations;
using GuestManagementService.Application.Invitations.GetInvitationSettings;
using GuestManagementService.Application.Invitations.SaveInvitationSettings;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Invitations;
using Moq;

namespace GuestManagementService.UnitTests.Invitations;

public sealed class InvitationSettingsTests
{
    private static readonly Guid EventId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");
    private static readonly Guid TenantId = Guid.Parse("0fa219ed-70ad-4e8d-9f51-6e60409dc659");
    private static readonly Guid OtherTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    // ---------- field schema ----------

    [Fact]
    public void Schema_WeddingCollectsCoupleNamesAndNoEventName()
    {
        var fields = InvitationFieldSchema.AllFor(InvitationFieldSchema.Wedding);

        Assert.Contains("brideName", fields);
        Assert.Contains("groomName", fields);
        Assert.DoesNotContain("eventName", fields);
    }

    [Fact]
    public void Schema_BirthdayCollectsEventNameAndNoCoupleNames()
    {
        var fields = InvitationFieldSchema.AllFor(InvitationFieldSchema.Birthday);

        Assert.Contains("eventName", fields);
        Assert.DoesNotContain("brideName", fields);
        Assert.DoesNotContain("groomName", fields);
    }

    [Fact]
    public void Schema_VenueNotesIsTheOnlyOptionalField()
    {
        foreach (var eventType in new[] { InvitationFieldSchema.Wedding, InvitationFieldSchema.Birthday })
        {
            var optional = InvitationFieldSchema.AllFor(eventType)
                .Except(InvitationFieldSchema.RequiredFor(eventType));

            Assert.Equal(new[] { "venueNotes" }, optional);
        }
    }

    [Fact]
    public void Schema_NeverCollectsGuestName()
    {
        // guestName is never typed — it comes from whichever guest's link is being opened.
        foreach (var eventType in new[] { InvitationFieldSchema.Wedding, InvitationFieldSchema.Birthday })
        {
            Assert.DoesNotContain("guestName", InvitationFieldSchema.AllFor(eventType));
        }
    }

    [Fact]
    public void Schema_RefusesAnUnsupportedEventType()
    {
        Assert.Throws<ValidationException>(() => InvitationFieldSchema.EnsureSupported("launch"));
    }

    // ---------- pre-fill ----------

    [Fact]
    public async Task Get_WhenNothingSaved_ReturnsEventDerivedDefaultsMarkedUnconfigured()
    {
        var result = await Get(saved: null);

        Assert.Equal(GetInvitationSettingsStatus.Found, result.Status);
        Assert.False(result.IsConfigured);
        Assert.Null(result.TemplateId);
        Assert.Equal("Villa Astoria", result.FieldValues!["venueName"]);
        Assert.Equal("Lake Como", result.FieldValues["venueAddress"]);
        // No event-record source for couple names — which is why the form exists.
        Assert.Null(result.FieldValues["brideName"]);
    }

    [Fact]
    public async Task Get_WhenSaved_ReturnsSavedValuesNotAFreshPreFill()
    {
        // AC 50: re-opening must show what was saved. Falling back to defaults would quietly
        // discard the organiser's edits every time they reopened the form.
        var saved = NewSnapshottedSettings("{\"venueName\":\"The Old Chapel\"}");

        var result = await Get(saved);

        Assert.True(result.IsConfigured);
        Assert.Equal(TemplateId.ToString(), result.TemplateId);
        Assert.Equal("The Old Chapel", result.FieldValues!["venueName"]);
    }

    [Fact]
    public async Task Get_ForAnotherTenantsEvent_ReturnsNotFound()
    {
        var result = await Get(saved: null, callerTenantId: OtherTenantId);

        Assert.Equal(GetInvitationSettingsStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Get_ForADeletedEvent_ReturnsNotFound()
    {
        var deleted = NewEvent();
        deleted.MarkDeleted(Now);

        var result = await Get(saved: null, eventReference: deleted);

        Assert.Equal(GetInvitationSettingsStatus.EventNotFound, result.Status);
    }

    // ---------- save: happy path ----------

    [Fact]
    public async Task Save_FetchesTheTemplateOnceAndSnapshotsItAlongsideFieldValues()
    {
        EventInvitationSettings? added = null;
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInvitationSettings?)null);
        settings
            .Setup(r => r.AddAsync(It.IsAny<EventInvitationSettings>(), It.IsAny<CancellationToken>()))
            .Callback<EventInvitationSettings, CancellationToken>((s, _) => added = s)
            .Returns(Task.CompletedTask);

        var catalog = CatalogReturning(TemplateFetchResult.Found(FoundTemplate()));

        var result = await Save(WeddingValues(), settings: settings.Object, catalog: catalog.Object);

        Assert.Equal(SaveInvitationSettingsStatus.Saved, result.Status);
        Assert.NotNull(added);
        Assert.Equal(TemplateId, added.TemplateId);
        Assert.Equal(3, added.TemplateVersion);
        Assert.Equal("<html><body>marigold v3</body></html>", added.HtmlContent);
        Assert.Contains("Amara", added.FieldValues, StringComparison.Ordinal);

        catalog.Verify(c => c.GetTemplateAsync(TemplateId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_ReSavingTheSameTemplateIsIdempotent()
    {
        // AC 12: fetching again and overwriting with identical content is fine — the important
        // property is that it never touches a DIFFERENT template unless explicitly re-chosen.
        var existing = NewSnapshottedSettings();
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var catalog = CatalogReturning(TemplateFetchResult.Found(FoundTemplate()));

        var result = await Save(WeddingValues(), settings: settings.Object, catalog: catalog.Object);

        Assert.Equal(SaveInvitationSettingsStatus.Saved, result.Status);
        Assert.Equal(TemplateId, existing.TemplateId);
        Assert.Equal(3, existing.TemplateVersion);
        Assert.Equal("<html><body>marigold v3</body></html>", existing.HtmlContent);
    }

    // ---------- save: snapshot failure branches (AC 10) ----------

    [Fact]
    public async Task Save_WhenTheCatalogIsUnreachable_RejectsWithBadRequestAndPersistsNothing()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var catalog = CatalogReturning(TemplateFetchResult.Unavailable());

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => Save(WeddingValues(), catalog: catalog.Object, unitOfWork: unitOfWork.Object));

        Assert.Contains(exception.Errors, e => e.PropertyName == "TemplateId");
        unitOfWork.Verify(w => w.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_WhenTheTemplateIsNotFound_RejectsWithBadRequestAndPersistsNothing()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var catalog = CatalogReturning(TemplateFetchResult.NotFound());

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => Save(WeddingValues(), catalog: catalog.Object, unitOfWork: unitOfWork.Object));

        Assert.Contains(exception.Errors, e => e.PropertyName == "TemplateId");
        unitOfWork.Verify(w => w.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_WhenTheTemplateFailsToParse_RejectsWithBadRequestAndPersistsNothing()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var unparseable = new TemplateCatalogEntry(
            TemplateId, "Marigold", "wedding", 3, "{{ if unterminated", null, null);
        var catalog = CatalogReturning(TemplateFetchResult.Found(unparseable));

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => Save(WeddingValues(), catalog: catalog.Object, unitOfWork: unitOfWork.Object));

        Assert.Contains(exception.Errors, e => e.PropertyName == "TemplateId");
        unitOfWork.Verify(w => w.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_RejectsAnInvalidGuidTemplateIdWithoutCallingTheCatalog()
    {
        var catalog = new Mock<ITemplateCatalogClient>();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => Save(WeddingValues(), templateId: "not-a-guid", catalog: catalog.Object));

        Assert.Contains(exception.Errors, e => e.PropertyName == "TemplateId");
        catalog.Verify(
            c => c.GetTemplateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- save: field validation (unchanged from slice 1) ----------

    [Fact]
    public async Task Save_RejectsAMissingRequiredField()
    {
        var values = WeddingValues();
        values.Remove("groomName");

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Save(values));

        Assert.Contains(exception.Errors, e => e.PropertyName == "groomName");
    }

    [Fact]
    public async Task Save_AcceptsAMissingVenueNotes()
    {
        // The one optional field. Blocking a save on "parking at the rear" would be a false gate.
        var values = WeddingValues();
        values.Remove("venueNotes");

        var result = await Save(values);

        Assert.Equal(SaveInvitationSettingsStatus.Saved, result.Status);
    }

    [Fact]
    public async Task Save_RejectsAFieldThatBelongsToADifferentEventType()
    {
        // eventName is a birthday field. Silently dropping it would let an organiser fill it in,
        // save successfully, and never see it on the invitation.
        var values = WeddingValues();
        values["eventName"] = "Not a wedding field";

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Save(values));

        Assert.Contains(exception.Errors, e => e.PropertyName == "eventName");
    }

    [Fact]
    public async Task Save_RejectsAnUnknownField()
    {
        var values = WeddingValues();
        values["favouriteColour"] = "terracotta";

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Save(values));

        Assert.Contains(exception.Errors, e => e.PropertyName == "favouriteColour");
    }

    [Fact]
    public async Task Save_RejectsAnOverLongValue()
    {
        var values = WeddingValues();
        values["venueAddress"] = new string('a', 501);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Save(values));

        Assert.Contains(exception.Errors, e => e.PropertyName == "venueAddress");
    }

    [Fact]
    public async Task Save_RejectsAMissingTemplate()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => Save(WeddingValues(), templateId: null));

        Assert.Contains(exception.Errors, e => e.PropertyName == "TemplateId");
    }

    [Fact]
    public async Task Save_ForAnotherTenantsEvent_ReturnsNotFoundAndWritesNothing()
    {
        var unitOfWork = new Mock<IUnitOfWork>();

        var result = await Save(WeddingValues(), callerTenantId: OtherTenantId, unitOfWork: unitOfWork.Object);

        Assert.Equal(SaveInvitationSettingsStatus.EventNotFound, result.Status);
        unitOfWork.Verify(w => w.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- save: public-link enable/disable folded in (was a separate endpoint) ----------

    [Fact]
    public async Task Save_WithPublicLinkEnabledTrue_MintsATokenOnFirstEnable()
    {
        var result = await Save(WeddingValues(), publicLinkEnabled: true);

        Assert.True(result.PublicLinkEnabled);
        Assert.False(string.IsNullOrEmpty(result.PublicEventToken));
    }

    [Fact]
    public async Task Save_WithPublicLinkEnabledTrue_ReusesTheExistingTokenIfAlreadyEnabled()
    {
        var existing = NewSnapshottedSettings();
        existing.EnablePublicLink(() => "already-live-token", Now);
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await Save(WeddingValues(), settings: settings.Object, publicLinkEnabled: true);

        // Re-saving other content while the link is already on must not silently rotate the URL.
        Assert.Equal("already-live-token", result.PublicEventToken);
    }

    [Fact]
    public async Task Save_WithPublicLinkEnabledFalse_DisablesAndRevokesTheToken()
    {
        var existing = NewSnapshottedSettings();
        existing.EnablePublicLink(() => "will-be-revoked", Now);
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await Save(WeddingValues(), settings: settings.Object, publicLinkEnabled: false);

        Assert.False(result.PublicLinkEnabled);
        Assert.Null(result.PublicEventToken);
    }

    [Fact]
    public async Task Save_WithPublicLinkEnabledOmitted_LeavesAnEnabledLinkUntouched()
    {
        // Omitted is a genuine no-op — leaving the field out of a request must never silently turn
        // a live public link off.
        var existing = NewSnapshottedSettings();
        existing.EnablePublicLink(() => "leave-me-alone", Now);
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await Save(WeddingValues(), settings: settings.Object, publicLinkEnabled: null);

        Assert.True(result.PublicLinkEnabled);
        Assert.Equal("leave-me-alone", result.PublicEventToken);
    }

    [Fact]
    public async Task Save_UpdatesExistingSettingsRatherThanInsertingASecondRow()
    {
        var existing = NewSnapshottedSettings();
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(EventId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await Save(WeddingValues(), settings: settings.Object);

        settings.Verify(r => r.AddAsync(It.IsAny<EventInvitationSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("Amara", existing.FieldValues, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_DropsAnythingOutsideTheEventTypesSchema()
    {
        // Defence in depth: even if validation were bypassed, storage only ever holds declared fields.
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["brideName"] = "Amara",
            ["favouriteColour"] = "terracotta",
        };

        var json = InvitationFieldValues.Serialize(values, InvitationFieldSchema.Wedding);

        Assert.Contains("brideName", json, StringComparison.Ordinal);
        Assert.DoesNotContain("favouriteColour", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_TreatsUnreadableStoredContentAsEmpty()
    {
        // Rather than throwing and taking down the organiser's form or a guest's page.
        Assert.Empty(InvitationFieldValues.Parse("{not json"));
        Assert.Empty(InvitationFieldValues.Parse(null));
    }

    // ---------- helpers ----------

    private static Dictionary<string, string?> WeddingValues() => new(StringComparer.Ordinal)
    {
        ["brideName"] = "Amara",
        ["groomName"] = "Julian",
        ["eventDate"] = "Saturday, September 12, 2026",
        ["eventTime"] = "18:30",
        ["venueName"] = "Villa Astoria",
        ["venueAddress"] = "Lake Como",
        ["venueNotes"] = "Parking at the rear",
    };

    private static TemplateCatalogEntry FoundTemplate() => new(
        TemplateId, "Marigold", "wedding", 3, "<html><body>marigold v3</body></html>", "body{}", null);

    private static Mock<ITemplateCatalogClient> CatalogReturning(TemplateFetchResult result)
    {
        var catalog = new Mock<ITemplateCatalogClient>();
        catalog
            .Setup(c => c.GetTemplateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return catalog;
    }

    private static EventInvitationSettings NewSnapshottedSettings(
        string fieldValues = "{}") =>
        EventInvitationSettings.Create(
            EventId, TenantId, fieldValues, TemplateId, 3, "<html><body>marigold v3</body></html>", "body{}", null, Now);

    private static async Task<GetInvitationSettingsResult> Get(
        EventInvitationSettings? saved,
        Guid? callerTenantId = null,
        EventReference? eventReference = null)
    {
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        var handler = new GetInvitationSettingsQueryHandler(Events(eventReference), settings.Object);

        return await handler.Handle(
            new GetInvitationSettingsQuery(EventId)
            {
                CurrentUser = new CurrentUser(Guid.NewGuid(), callerTenantId ?? TenantId),
            },
            CancellationToken.None);
    }

    private const string UseDefaultTemplateId = "__use-default-template-id__";

    private static async Task<SaveInvitationSettingsResult> Save(
        Dictionary<string, string?> values,
        string? templateId = UseDefaultTemplateId,
        Guid? callerTenantId = null,
        IEventInvitationSettingsRepository? settings = null,
        IUnitOfWork? unitOfWork = null,
        ITemplateCatalogClient? catalog = null,
        bool? publicLinkEnabled = null)
    {
        if (templateId == UseDefaultTemplateId)
        {
            templateId = TemplateId.ToString();
        }

        var repo = settings ?? DefaultSettingsRepository();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(p => p.GetUtcNow()).Returns(Now);

        var tokenGenerator = new Mock<IInvitationTokenGenerator>();
        tokenGenerator.Setup(g => g.Generate()).Returns(() => Guid.NewGuid().ToString("N"));

        var handler = new SaveInvitationSettingsCommandHandler(
            Events(null),
            repo,
            catalog ?? CatalogReturning(TemplateFetchResult.Found(FoundTemplate())).Object,
            tokenGenerator.Object,
            unitOfWork ?? new Mock<IUnitOfWork>().Object,
            timeProvider.Object);

        return await handler.Handle(
            new SaveInvitationSettingsCommand(EventId, templateId, values, publicLinkEnabled)
            {
                CurrentUser = new CurrentUser(Guid.NewGuid(), callerTenantId ?? TenantId),
            },
            CancellationToken.None);
    }

    private static IEventInvitationSettingsRepository DefaultSettingsRepository()
    {
        var settings = new Mock<IEventInvitationSettingsRepository>();
        settings
            .Setup(r => r.GetByEventIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInvitationSettings?)null);

        return settings.Object;
    }

    private static IEventReferenceRepository Events(EventReference? eventReference)
    {
        var events = new Mock<IEventReferenceRepository>();
        events
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventReference ?? NewEvent());

        return events.Object;
    }

    private static EventReference NewEvent()
    {
        return EventReference.Active(
            EventId,
            "Amara & Julian",
            TenantId,
            Now,
            "wedding",
            new DateOnly(2026, 9, 12),
            new TimeOnly(18, 30),
            "Asia/Ho_Chi_Minh",
            "Villa Astoria",
            "Lake Como",
            null);
    }
}
