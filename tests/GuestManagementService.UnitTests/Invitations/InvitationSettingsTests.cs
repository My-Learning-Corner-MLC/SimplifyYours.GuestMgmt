using FluentValidation;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
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
        var saved = EventInvitationSettings.Create(
            EventId, TenantId, "marigold", "{\"venueName\":\"The Old Chapel\"}", Now);

        var result = await Get(saved);

        Assert.True(result.IsConfigured);
        Assert.Equal("marigold", result.TemplateId);
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

    // ---------- save ----------

    [Fact]
    public async Task Save_PersistsTemplateAndValues()
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

        var result = await Save(WeddingValues(), settings: settings.Object);

        Assert.Equal(SaveInvitationSettingsStatus.Saved, result.Status);
        Assert.NotNull(added);
        Assert.Equal("marigold", added.TemplateId);
        Assert.Contains("Amara", added.FieldValues, StringComparison.Ordinal);
    }

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

    [Fact]
    public async Task Save_UpdatesExistingSettingsRatherThanInsertingASecondRow()
    {
        var existing = EventInvitationSettings.Create(EventId, TenantId, "marigold", "{}", Now);
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

    private static async Task<SaveInvitationSettingsResult> Save(
        Dictionary<string, string?> values,
        string? templateId = "marigold",
        Guid? callerTenantId = null,
        IEventInvitationSettingsRepository? settings = null,
        IUnitOfWork? unitOfWork = null)
    {
        var repo = settings ?? DefaultSettingsRepository();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(p => p.GetUtcNow()).Returns(Now);

        var handler = new SaveInvitationSettingsCommandHandler(
            Events(null),
            repo,
            unitOfWork ?? new Mock<IUnitOfWork>().Object,
            timeProvider.Object);

        return await handler.Handle(
            new SaveInvitationSettingsCommand(EventId, templateId, values)
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
        var reference = EventReference.Active(EventId, "Amara & Julian", TenantId, Now, "wedding");
        reference.ApplyDisplayDetails(
            new DateOnly(2026, 9, 12),
            new TimeOnly(18, 30),
            null,
            "Asia/Ho_Chi_Minh",
            null,
            "Villa Astoria",
            "Lake Como",
            null);

        return reference;
    }
}
