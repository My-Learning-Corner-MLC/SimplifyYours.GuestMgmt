using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.EventReferences.ApplyEventReferenceEvent;
using GuestManagementService.Domain.EventReferences;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.EventReferences;

public sealed class ApplyEventReferenceEventCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("0fa219ed-70ad-4e8d-9f51-6e60409dc659");

    [Fact]
    public async Task Handle_WhenEventCreated_UpsertsActiveReference()
    {
        EventReference? savedReference = null;
        var references = new Mock<IEventReferenceRepository>();
        references
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventReference?)null);
        references
            .Setup(repository => repository.UpsertAsync(It.IsAny<EventReference>(), It.IsAny<CancellationToken>()))
            .Callback<EventReference, CancellationToken>((reference, _) => savedReference = reference)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(references.Object, unitOfWork: unitOfWork.Object);

        var applied = await handler.Handle(ValidCommand("EventCreated"), CancellationToken.None);

        Assert.True(applied);
        Assert.NotNull(savedReference);
        Assert.False(savedReference.IsDeleted);
        Assert.Equal("Launch", savedReference.EventName);
        Assert.Equal(TenantId, savedReference.TenantId);
        Assert.Equal("wedding", savedReference.EventType);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventUpdated_RefreshesPlannedEventType()
    {
        var eventId = Guid.NewGuid();
        var existing = EventReference.Active(eventId, "Launch", TenantId, DateTimeOffset.UtcNow, "birthday");
        var references = new Mock<IEventReferenceRepository>();
        references
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var handler = CreateHandler(references.Object);

        var applied = await handler.Handle(
            new ApplyEventReferenceEventCommand(
                Guid.NewGuid(),
                "EventUpdated",
                eventId,
                "Launch",
                DateTimeOffset.UtcNow,
                "wedding",
                TenantId),
            CancellationToken.None);

        Assert.True(applied);
        Assert.Equal("wedding", existing.EventType);
    }

    [Fact]
    public async Task Handle_WhenEventDeleted_MarksReferenceDeleted()
    {
        var eventId = Guid.NewGuid();
        var existing = EventReference.Active(eventId, "Launch", TenantId, DateTimeOffset.UtcNow, "wedding");
        var references = new Mock<IEventReferenceRepository>();
        references
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var handler = CreateHandler(references.Object);

        var applied = await handler.Handle(new ApplyEventReferenceEventCommand(
            Guid.NewGuid(),
            "EventDeleted",
            eventId,
            "Launch",
            DateTimeOffset.UtcNow,
            "wedding",
            TenantId),
            CancellationToken.None);

        Assert.True(applied);
        Assert.True(existing.IsDeleted);
    }

    [Fact]
    public async Task Handle_WhenEventCreated_StoresDisplayFieldsForTheInvitationPage()
    {
        EventReference? savedReference = null;
        var references = new Mock<IEventReferenceRepository>();
        references
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventReference?)null);
        references
            .Setup(repository => repository.UpsertAsync(It.IsAny<EventReference>(), It.IsAny<CancellationToken>()))
            .Callback<EventReference, CancellationToken>((reference, _) => savedReference = reference)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(references.Object);

        var applied = await handler.Handle(
            ValidCommand("EventCreated") with
            {
                EventDate = new DateOnly(2026, 9, 12),
                EventStartTime = new TimeOnly(18, 30),
                TimeZoneId = "Asia/Ho_Chi_Minh",
                EventDescription = "  An evening reception  ",
                VenueName = "Rosewood Hall",
                VenueAddress = "12 Sample Street",
                VenueNotes = "  ",
            },
            CancellationToken.None);

        Assert.True(applied);
        Assert.NotNull(savedReference);
        Assert.Equal(new DateOnly(2026, 9, 12), savedReference.EventDate);
        Assert.Equal(new TimeOnly(18, 30), savedReference.EventStartTime);
        Assert.Equal("Asia/Ho_Chi_Minh", savedReference.TimeZoneId);
        Assert.Equal("An evening reception", savedReference.EventDescription);
        Assert.Equal("Rosewood Hall", savedReference.VenueName);
        Assert.Equal("12 Sample Street", savedReference.VenueAddress);
        Assert.Null(savedReference.VenueNotes);
    }

    [Fact]
    public async Task Handle_WhenEventUpdated_ReplacesDisplayFieldsWithTheIncomingPayload()
    {
        var eventId = Guid.NewGuid();
        var existing = EventReference.Active(
            eventId,
            "Launch",
            TenantId,
            DateTimeOffset.UtcNow,
            "wedding",
            new DateOnly(2026, 9, 12),
            new TimeOnly(18, 30),
            "Asia/Ho_Chi_Minh",
            "An evening reception",
            "Rosewood Hall",
            "12 Sample Street",
            null);
        var references = new Mock<IEventReferenceRepository>();
        references
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var handler = CreateHandler(references.Object);

        var applied = await handler.Handle(
            new ApplyEventReferenceEventCommand(
                Guid.NewGuid(),
                "EventUpdated",
                eventId,
                "Launch",
                DateTimeOffset.UtcNow,
                "wedding",
                TenantId,
                EventDate: new DateOnly(2026, 9, 12)),
            CancellationToken.None);

        Assert.True(applied);
        Assert.Null(existing.VenueName);
        Assert.Null(existing.VenueAddress);
        Assert.Null(existing.TimeZoneId);
        Assert.Equal(new DateOnly(2026, 9, 12), existing.EventDate);
    }

    private static ApplyEventReferenceEventCommand ValidCommand(string eventType)
    {
        return new ApplyEventReferenceEventCommand(
            Guid.NewGuid(),
            eventType,
            Guid.NewGuid(),
            "Launch",
            DateTimeOffset.UtcNow,
            "wedding",
            TenantId);
    }

    private static ApplyEventReferenceEventCommandHandler CreateHandler(
        IEventReferenceRepository? eventReferenceRepository = null,
        IUnitOfWork? unitOfWork = null)
    {
        var references = new Mock<IEventReferenceRepository>();
        var work = new Mock<IUnitOfWork>();

        return new ApplyEventReferenceEventCommandHandler(
            eventReferenceRepository ?? references.Object,
            unitOfWork ?? work.Object,
            NullLogger<ApplyEventReferenceEventCommandHandler>.Instance);
    }
}
