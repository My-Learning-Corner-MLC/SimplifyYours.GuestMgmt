using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Seating.ApplyTablePositionsBatch;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Seating;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Seating.ApplyTablePositionsBatch;

public sealed class ApplyTablePositionsBatchCommandHandlerTests
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
            Command(Guid.NewGuid(), [new TablePositionInput(Guid.NewGuid(), 0, 0, 0)]),
            CancellationToken.None);

        Assert.Equal(ApplyTablePositionsBatchStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenAllTablesExist_MovesThemInOneSave()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var tableA = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var tableB = layout.AddTable(Guid.NewGuid(), "Friends", TableShape.Round, 8, Now);
        var provisioner = LayoutProvisioner(eventId, layout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var positions = new[]
        {
            new TablePositionInput(tableA.Id, 10, 20, 0),
            new TablePositionInput(tableB.Id, 30, 40, 90)
        };

        var result = await handler.Handle(Command(eventId, positions), CancellationToken.None);

        Assert.Equal(ApplyTablePositionsBatchStatus.Applied, result.Status);
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, r => Assert.Equal(TablePositionOpStatus.Applied, r.Status));
        Assert.Equal(10, tableA.PositionX);
        Assert.Equal(30, tableB.PositionX);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenATableIsMissing_ReportsTableNotFoundButStillSavesTheRest()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var provisioner = LayoutProvisioner(eventId, layout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var positions = new[]
        {
            new TablePositionInput(table.Id, 10, 20, 0),
            new TablePositionInput(Guid.NewGuid(), 30, 40, 0)
        };

        var result = await handler.Handle(Command(eventId, positions), CancellationToken.None);

        Assert.Equal(TablePositionOpStatus.Applied, result.Results[0].Status);
        Assert.Equal(TablePositionOpStatus.TableNotFound, result.Results[1].Status);
        Assert.Equal(10, table.PositionX);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ApplyTablePositionsBatchCommand Command(Guid eventId, IReadOnlyList<TablePositionInput> positions)
    {
        return new ApplyTablePositionsBatchCommand(eventId, positions) { CurrentUser = TestUser };
    }

    private static Mock<ISeatingLayoutProvisioner> LayoutProvisioner(Guid eventId, SeatingLayout layout)
    {
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        return provisioner;
    }

    private static ApplyTablePositionsBatchCommandHandler CreateHandler(
        IEventReferenceRepository? eventReferenceRepository = null,
        ISeatingLayoutProvisioner? provisioner = null,
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

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(Now);

        return new ApplyTablePositionsBatchCommandHandler(
            eventReferenceRepository ?? eventReferences.Object,
            provisioner ?? provisioners.Object,
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            timeProvider.Object,
            NullLogger<ApplyTablePositionsBatchCommandHandler>.Instance);
    }
}
