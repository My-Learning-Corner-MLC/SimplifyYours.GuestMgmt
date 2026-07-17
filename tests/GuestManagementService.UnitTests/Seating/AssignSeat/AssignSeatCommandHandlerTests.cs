using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Seating.AssignSeat;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Seating;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Seating.AssignSeat;

public sealed class AssignSeatCommandHandlerTests
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

        var result = await handler.Handle(Command(Guid.NewGuid(), Guid.NewGuid(), 0, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AssignSeatStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenTableDoesNotExist_ReturnsTableNotFound()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var provisioner = LayoutProvisioner(eventId, layout);
        var handler = CreateHandler(provisioner: provisioner.Object, eventId: eventId);

        var result = await handler.Handle(Command(eventId, Guid.NewGuid(), 0, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AssignSeatStatus.TableNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenGuestDoesNotBelongToEvent_ReturnsGuestNotFound()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ExistsAsync(eventId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(Command(eventId, table.Id, 0, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AssignSeatStatus.GuestNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenSeatIndexOutOfRange_ReturnsSeatIndexOutOfRange()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var provisioner = LayoutProvisioner(eventId, layout);
        var handler = CreateHandler(provisioner: provisioner.Object, eventId: eventId);

        var result = await handler.Handle(Command(eventId, table.Id, 8, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AssignSeatStatus.SeatIndexOutOfRange, result.Status);
    }

    [Fact]
    public async Task Handle_WhenSeatIsFree_AssignsAndSaves()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var provisioner = LayoutProvisioner(eventId, layout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var guestId = Guid.NewGuid();
        var handler = CreateHandler(provisioner: provisioner.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var result = await handler.Handle(Command(eventId, table.Id, 3, guestId), CancellationToken.None);

        Assert.Equal(AssignSeatStatus.Assigned, result.Status);
        Assert.NotNull(result.Table);
        Assert.Equal(guestId, result.Table.Seats[3].GuestId);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSeatOccupiedByAnotherGuest_ReturnsSeatOccupiedAndDoesNotSave()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        layout.AssignGuest(table.Id, 0, Guid.NewGuid(), Now, out _);
        var provisioner = LayoutProvisioner(eventId, layout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var result = await handler.Handle(Command(eventId, table.Id, 0, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AssignSeatStatus.SeatOccupied, result.Status);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AssignSeatCommand Command(Guid eventId, Guid tableId, int seatIndex, Guid guestId)
    {
        return new AssignSeatCommand(eventId, tableId, seatIndex, guestId) { CurrentUser = TestUser };
    }

    private static Mock<ISeatingLayoutProvisioner> LayoutProvisioner(Guid eventId, SeatingLayout layout)
    {
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        return provisioner;
    }

    private static AssignSeatCommandHandler CreateHandler(
        IEventReferenceRepository? eventReferenceRepository = null,
        ISeatingLayoutProvisioner? provisioner = null,
        IGuestRepository? guestRepository = null,
        IUnitOfWork? unitOfWork = null,
        Guid? eventId = null)
    {
        var resolvedEventId = eventId ?? Guid.NewGuid();

        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(resolvedEventId, "Launch", TestTenantId, Now, "wedding"));

        var provisioners = new Mock<ISeatingLayoutProvisioner>();
        provisioners
            .Setup(p => p.GetOrCreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeatingLayout.Create(Guid.NewGuid(), resolvedEventId, TestTenantId, Now));

        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(Now);

        return new AssignSeatCommandHandler(
            eventReferenceRepository ?? eventReferences.Object,
            provisioner ?? provisioners.Object,
            guestRepository ?? guests.Object,
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            timeProvider.Object,
            NullLogger<AssignSeatCommandHandler>.Instance);
    }
}
