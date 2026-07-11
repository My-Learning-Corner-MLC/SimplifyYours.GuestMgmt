using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Seating.GetSeatingLayout;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using GuestManagementService.Domain.Seating;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Seating.GetSeatingLayout;

public sealed class GetSeatingLayoutQueryHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("ff3d23f3-6a5e-4555-b189-630dfd24bad8");
    private static readonly Guid TestTenantId = Guid.Parse("c2b9d3a1-4d4b-4b1a-9bc4-2f5a7e8d9f01");
    private static readonly CurrentUser TestUser = new(TestUserId, TestTenantId);
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WhenEventIsMissing_ReturnsEventNotFound()
    {
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventReference?)null);
        var handler = CreateHandler(eventReferenceRepository: eventReferences.Object);

        var result = await handler.Handle(Query(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(GetSeatingLayoutStatus.EventNotFound, result.Status);
        Assert.Null(result.Layout);
    }

    [Fact]
    public async Task Handle_WhenEventIsDeleted_ReturnsEventNotFound()
    {
        var eventId = Guid.NewGuid();
        var deletedReference = EventReference.Active(eventId, "Launch", TestTenantId, Now);
        deletedReference.MarkDeleted(Now);
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedReference);
        var handler = CreateHandler(eventReferenceRepository: eventReferences.Object);

        var result = await handler.Handle(Query(eventId), CancellationToken.None);

        Assert.Equal(GetSeatingLayoutStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenEventBelongsToAnotherTenant_ReturnsEventNotFound()
    {
        var eventId = Guid.NewGuid();
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Launch", Guid.NewGuid(), Now));
        var handler = CreateHandler(eventReferenceRepository: eventReferences.Object);

        var result = await handler.Handle(Query(eventId), CancellationToken.None);

        Assert.Equal(GetSeatingLayoutStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenNoLayoutExists_CreatesLayoutAndPersistsIt()
    {
        var eventId = Guid.NewGuid();
        SeatingLayout? savedLayout = null;
        var seatingLayouts = new Mock<ISeatingLayoutRepository>();
        seatingLayouts
            .Setup(repository => repository.GetByEventAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeatingLayout?)null);
        seatingLayouts
            .Setup(repository => repository.AddAsync(It.IsAny<SeatingLayout>(), It.IsAny<CancellationToken>()))
            .Callback<SeatingLayout, CancellationToken>((layout, _) => savedLayout = layout)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(seatingLayoutRepository: seatingLayouts.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var result = await handler.Handle(Query(eventId), CancellationToken.None);

        Assert.Equal(GetSeatingLayoutStatus.Found, result.Status);
        Assert.NotNull(result.Layout);
        Assert.Equal(eventId, result.Layout.EventId);
        Assert.Empty(result.Layout.Tables);
        Assert.NotNull(savedLayout);
        Assert.Equal(TestTenantId, savedLayout.TenantId);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLayoutExists_DoesNotCreateANewOne()
    {
        var eventId = Guid.NewGuid();
        var existingLayout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var seatingLayouts = new Mock<ISeatingLayoutRepository>();
        seatingLayouts
            .Setup(repository => repository.GetByEventAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLayout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(seatingLayoutRepository: seatingLayouts.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var result = await handler.Handle(Query(eventId), CancellationToken.None);

        Assert.Equal(GetSeatingLayoutStatus.Found, result.Status);
        Assert.NotNull(result.Layout);
        Assert.Equal(existingLayout.Id, result.Layout.Id);
        seatingLayouts.Verify(
            repository => repository.AddAsync(It.IsAny<SeatingLayout>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProjectsTablesAndComputesSummary()
    {
        var eventId = Guid.NewGuid();
        var existingLayout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        existingLayout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        existingLayout.AddTable(Guid.NewGuid(), "Friends", TableShape.Long, 6, Now);
        var seatingLayouts = new Mock<ISeatingLayoutRepository>();
        seatingLayouts
            .Setup(repository => repository.GetByEventAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLayout);
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ListByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FiveGuests(eventId));
        var handler = CreateHandler(seatingLayoutRepository: seatingLayouts.Object, guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(Query(eventId), CancellationToken.None);

        Assert.NotNull(result.Layout);
        Assert.Equal(2, result.Layout.Tables.Count);
        Assert.Equal(2, result.Layout.Summary.TableCount);
        Assert.Equal(14, result.Layout.Summary.SeatCount);
        Assert.Equal(0, result.Layout.Summary.SeatedCount);
        Assert.Equal(5, result.Layout.Summary.FloatingCount);
        var roundTable = result.Layout.Tables.Single(table => table.Name == "Family");
        Assert.Equal("Round", roundTable.Shape);
        Assert.Equal(8, roundTable.SeatCount);
        Assert.False(roundTable.IsFull);
    }

    private static IReadOnlyList<Guest> FiveGuests(Guid eventId)
    {
        return Enumerable.Range(0, 5)
            .Select(index => Guest.Create(
                Guid.NewGuid(),
                eventId,
                TestTenantId,
                $"Guest{index}",
                "Tester",
                $"+1555000000{index}",
                $"+1555000000{index}",
                null,
                null,
                Gender.PreferNotToSay,
                null,
                Now))
            .ToList();
    }

    private static GetSeatingLayoutQuery Query(Guid eventId)
    {
        return new GetSeatingLayoutQuery(eventId) { CurrentUser = TestUser };
    }

    private static GetSeatingLayoutQueryHandler CreateHandler(
        IEventReferenceRepository? eventReferenceRepository = null,
        ISeatingLayoutRepository? seatingLayoutRepository = null,
        IGuestRepository? guestRepository = null,
        IUnitOfWork? unitOfWork = null,
        Guid? eventId = null)
    {
        var resolvedEventId = eventId ?? Guid.NewGuid();

        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(resolvedEventId, "Launch", TestTenantId, Now));

        var seatingLayouts = new Mock<ISeatingLayoutRepository>();
        seatingLayouts
            .Setup(repository => repository.GetByEventAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeatingLayout.Create(Guid.NewGuid(), resolvedEventId, TestTenantId, Now));

        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ListByEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guest>());

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(Now);

        return new GetSeatingLayoutQueryHandler(
            eventReferenceRepository ?? eventReferences.Object,
            seatingLayoutRepository ?? seatingLayouts.Object,
            guestRepository ?? guests.Object,
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            timeProvider.Object,
            NullLogger<GetSeatingLayoutQueryHandler>.Instance);
    }
}
