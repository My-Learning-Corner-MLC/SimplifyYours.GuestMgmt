using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Guests.AddGuest;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Guests.AddGuest;

public sealed class AddGuestCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEventExists_CreatesGuest()
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        Guest? savedGuest = null;
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Launch", now));
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()))
            .Callback<Guest, CancellationToken>((guest, _) => savedGuest = guest)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);
        var handler = new AddGuestCommandHandler(
            eventReferences.Object,
            guests.Object,
            unitOfWork.Object,
            timeProvider.Object,
            NullLogger<AddGuestCommandHandler>.Instance);

        var result = await handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Lovelace",
            "+1 555 123 4567",
            "ADA@EXAMPLE.COM",
            null),
            CancellationToken.None);

        Assert.Equal(AddGuestStatus.Created, result.Status);
        Assert.NotNull(result.Guest);
        Assert.Equal(eventId, result.Guest.EventId);
        Assert.Equal("preferNotToSay", result.Guest.Gender);
        Assert.Equal("ADA@EXAMPLE.COM", result.Guest.EmailAddress);
        Assert.NotNull(savedGuest);
        Assert.Equal("+15551234567", savedGuest.NormalizedPhoneNumber);
        Assert.Equal("ada@example.com", savedGuest.NormalizedEmailAddress);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventIsMissing_ReturnsEventNotFound()
    {
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventReference?)null);
        var handler = CreateHandler(eventReferences.Object);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AddGuestStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenPhoneIsDuplicate_ReturnsDuplicate()
    {
        var eventId = Guid.NewGuid();
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ExistsByPhoneAsync(eventId, "+15551234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler(guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(ValidCommand(eventId), CancellationToken.None);

        Assert.Equal(AddGuestStatus.Duplicate, result.Status);
    }

    [Fact]
    public async Task Handle_WhenProvidedEmailIsDuplicate_ReturnsDuplicate()
    {
        var eventId = Guid.NewGuid();
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ExistsByEmailAsync(eventId, "ada@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler(guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(ValidCommand(eventId), CancellationToken.None);

        Assert.Equal(AddGuestStatus.Duplicate, result.Status);
    }

    [Fact]
    public async Task Handle_WhenEmailIsOmitted_DoesNotCheckDuplicateEmail()
    {
        var eventId = Guid.NewGuid();
        var guests = new Mock<IGuestRepository>();
        var handler = CreateHandler(guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Lovelace",
            "+1 555 123 4567",
            null,
            "female"),
            CancellationToken.None);

        Assert.Equal(AddGuestStatus.Created, result.Status);
        guests.Verify(
            repository => repository.ExistsByEmailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AddGuestCommand ValidCommand(Guid eventId)
    {
        return new AddGuestCommand(eventId, "Ada", "Lovelace", "+1 555 123 4567", "ADA@example.com", "female");
    }

    private static AddGuestCommandHandler CreateHandler(
        IEventReferenceRepository? eventReferenceRepository = null,
        IGuestRepository? guestRepository = null,
        Guid? eventId = null)
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var resolvedEventId = eventId ?? Guid.NewGuid();
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(resolvedEventId, "Launch", now));
        var guests = new Mock<IGuestRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);

        return new AddGuestCommandHandler(
            eventReferenceRepository ?? eventReferences.Object,
            guestRepository ?? guests.Object,
            unitOfWork.Object,
            timeProvider.Object,
            NullLogger<AddGuestCommandHandler>.Instance);
    }
}
