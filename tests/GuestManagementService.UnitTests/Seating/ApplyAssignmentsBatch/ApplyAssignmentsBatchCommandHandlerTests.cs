using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Seating;
using GuestManagementService.Application.Seating.ApplyAssignmentsBatch;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using GuestManagementService.Domain.Seating;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Seating.ApplyAssignmentsBatch;

public sealed class ApplyAssignmentsBatchCommandHandlerTests
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

        var result = await handler.Handle(
            Command(Guid.NewGuid(), [new SeatingBatchOpInput(SeatingBatchOpType.Unassign, Guid.NewGuid(), null, null)]),
            CancellationToken.None);

        Assert.Equal(ApplyAssignmentsBatchStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenAssignOpValid_AppliesItAndSavesOnce()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var guest = MakeGuest(eventId);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = GuestRepo(eventId, [guest]);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var result = await handler.Handle(
            Command(eventId, [new SeatingBatchOpInput(SeatingBatchOpType.Assign, guest.Id, table.Id, 0)]),
            CancellationToken.None);

        Assert.Equal(ApplyAssignmentsBatchStatus.Applied, result.Status);
        var opResult = Assert.Single(result.OpResults);
        Assert.Equal(SeatingBatchOpStatus.Applied, opResult.Status);
        Assert.Single(layout.Assignments);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUnassignOpTargetsSeatedGuest_RemovesThem()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var guest = MakeGuest(eventId);
        layout.AssignGuest(table.Id, 0, guest.Id, Now, out _);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = GuestRepo(eventId, [guest]);
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(
            Command(eventId, [new SeatingBatchOpInput(SeatingBatchOpType.Unassign, guest.Id, null, null)]),
            CancellationToken.None);

        Assert.Equal(SeatingBatchOpStatus.Applied, Assert.Single(result.OpResults).Status);
        Assert.Empty(layout.Assignments);
    }

    [Fact]
    public async Task Handle_WhenAssignOpGuestNotInEvent_ReportsGuestNotFoundAndDoesNotAssign()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = GuestRepo(eventId, []);
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(
            Command(eventId, [new SeatingBatchOpInput(SeatingBatchOpType.Assign, Guid.NewGuid(), table.Id, 0)]),
            CancellationToken.None);

        Assert.Equal(SeatingBatchOpStatus.GuestNotFound, Assert.Single(result.OpResults).Status);
        Assert.Empty(layout.Assignments);
    }

    [Fact]
    public async Task Handle_WhenAssignOpTableMissing_ReportsTableNotFound()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var guest = MakeGuest(eventId);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = GuestRepo(eventId, [guest]);
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(
            Command(eventId, [new SeatingBatchOpInput(SeatingBatchOpType.Assign, guest.Id, Guid.NewGuid(), 0)]),
            CancellationToken.None);

        Assert.Equal(SeatingBatchOpStatus.TableNotFound, Assert.Single(result.OpResults).Status);
    }

    [Fact]
    public async Task Handle_WhenAssignOpSeatIndexOutOfRange_ReportsSeatIndexOutOfRange()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var guest = MakeGuest(eventId);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = GuestRepo(eventId, [guest]);
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(
            Command(eventId, [new SeatingBatchOpInput(SeatingBatchOpType.Assign, guest.Id, table.Id, 8)]),
            CancellationToken.None);

        Assert.Equal(SeatingBatchOpStatus.SeatIndexOutOfRange, Assert.Single(result.OpResults).Status);
    }

    [Fact]
    public async Task Handle_WhenAssignOpSeatOccupiedByAnotherGuest_ReportsConflict()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var occupant = MakeGuest(eventId);
        var mover = MakeGuest(eventId);
        layout.AssignGuest(table.Id, 0, occupant.Id, Now, out _);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = GuestRepo(eventId, [occupant, mover]);
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(
            Command(eventId, [new SeatingBatchOpInput(SeatingBatchOpType.Assign, mover.Id, table.Id, 0)]),
            CancellationToken.None);

        Assert.Equal(SeatingBatchOpStatus.Conflict, Assert.Single(result.OpResults).Status);
        Assert.Equal(occupant.Id, layout.Assignments.Single().GuestId);
    }

    [Fact]
    public async Task Handle_MixedBatch_AppliesValidOpsReportsInvalidOnesInOneSave()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var validGuest = MakeGuest(eventId);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = GuestRepo(eventId, [validGuest]);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var ops = new[]
        {
            new SeatingBatchOpInput(SeatingBatchOpType.Assign, validGuest.Id, table.Id, 0),
            new SeatingBatchOpInput(SeatingBatchOpType.Assign, Guid.NewGuid(), table.Id, 1)
        };

        var result = await handler.Handle(Command(eventId, ops), CancellationToken.None);

        Assert.Equal(2, result.OpResults.Count);
        Assert.Equal(SeatingBatchOpStatus.Applied, result.OpResults[0].Status);
        Assert.Equal(SeatingBatchOpStatus.GuestNotFound, result.OpResults[1].Status);
        Assert.Single(layout.Assignments);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsProjectedLayoutWithUpdatedSummary()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var guest = MakeGuest(eventId);
        var provisioner = LayoutProvisioner(eventId, layout);
        var guests = GuestRepo(eventId, [guest]);
        var handler = CreateHandler(provisioner: provisioner.Object, guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(
            Command(eventId, [new SeatingBatchOpInput(SeatingBatchOpType.Assign, guest.Id, table.Id, 0)]),
            CancellationToken.None);

        Assert.NotNull(result.Layout);
        Assert.Equal(1, result.Layout.Summary.SeatedCount);
        Assert.Equal(0, result.Layout.Summary.FloatingCount);
    }

    private static Guest MakeGuest(Guid eventId)
    {
        return Guest.Create(
            Guid.NewGuid(),
            eventId,
            TestTenantId,
            "Ada",
            "Tester",
            "+15551234567",
            "+15551234567",
            null,
            null,
            Gender.PreferNotToSay,
            null,
            Now);
    }

    private static ApplyAssignmentsBatchCommand Command(Guid eventId, IReadOnlyList<SeatingBatchOpInput> ops)
    {
        return new ApplyAssignmentsBatchCommand(eventId, ops) { CurrentUser = TestUser };
    }

    private static Mock<ISeatingLayoutProvisioner> LayoutProvisioner(Guid eventId, SeatingLayout layout)
    {
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        return provisioner;
    }

    private static Mock<IGuestRepository> GuestRepo(Guid eventId, IReadOnlyList<Guest> guestList)
    {
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ListByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestList);
        return guests;
    }

    private static ApplyAssignmentsBatchCommandHandler CreateHandler(
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
            .ReturnsAsync(EventReference.Active(resolvedEventId, "Launch", TestTenantId, Now));

        var provisioners = new Mock<ISeatingLayoutProvisioner>();
        provisioners
            .Setup(p => p.GetOrCreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeatingLayout.Create(Guid.NewGuid(), resolvedEventId, TestTenantId, Now));

        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ListByEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guest>());

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(Now);

        return new ApplyAssignmentsBatchCommandHandler(
            eventReferenceRepository ?? eventReferences.Object,
            provisioner ?? provisioners.Object,
            guestRepository ?? guests.Object,
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            timeProvider.Object,
            NullLogger<ApplyAssignmentsBatchCommandHandler>.Instance);
    }
}
