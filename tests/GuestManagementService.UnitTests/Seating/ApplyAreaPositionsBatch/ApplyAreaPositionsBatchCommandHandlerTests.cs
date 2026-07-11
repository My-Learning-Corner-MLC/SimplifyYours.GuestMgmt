using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Seating.ApplyAreaPositionsBatch;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Seating;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Seating.ApplyAreaPositionsBatch;

public sealed class ApplyAreaPositionsBatchCommandHandlerTests
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
            Command(Guid.NewGuid(), [new AreaPositionInput(Guid.NewGuid(), 0, 0, 0)]),
            CancellationToken.None);

        Assert.Equal(ApplyAreaPositionsBatchStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenAllAreasExist_MovesThemInOneSave()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var areaA = layout.AddArea(Guid.NewGuid(), "Stage", AreaKind.Stage, AreaShape.Rect, 3.4, 0.9, null, null, Now);
        var areaB = layout.AddArea(Guid.NewGuid(), "Bar", AreaKind.Bar, AreaShape.Rect, 1.5, 0.6, null, null, Now);
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var positions = new[]
        {
            new AreaPositionInput(areaA.Id, 10, 20, 0),
            new AreaPositionInput(areaB.Id, 30, 40, 90)
        };

        var result = await handler.Handle(Command(eventId, positions), CancellationToken.None);

        Assert.Equal(ApplyAreaPositionsBatchStatus.Applied, result.Status);
        Assert.All(result.Results, r => Assert.Equal(AreaPositionOpStatus.Applied, r.Status));
        Assert.Equal(10, areaA.PositionX);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAnAreaIsMissing_ReportsAreaNotFoundButStillSavesTheRest()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var area = layout.AddArea(Guid.NewGuid(), "Stage", AreaKind.Stage, AreaShape.Rect, 3.4, 0.9, null, null, Now);
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var positions = new[]
        {
            new AreaPositionInput(area.Id, 10, 20, 0),
            new AreaPositionInput(Guid.NewGuid(), 30, 40, 0)
        };

        var result = await handler.Handle(Command(eventId, positions), CancellationToken.None);

        Assert.Equal(AreaPositionOpStatus.Applied, result.Results[0].Status);
        Assert.Equal(AreaPositionOpStatus.AreaNotFound, result.Results[1].Status);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ApplyAreaPositionsBatchCommand Command(Guid eventId, IReadOnlyList<AreaPositionInput> positions)
    {
        return new ApplyAreaPositionsBatchCommand(eventId, positions) { CurrentUser = TestUser };
    }

    private static ApplyAreaPositionsBatchCommandHandler CreateHandler(
        IEventReferenceRepository? eventReferenceRepository = null,
        ISeatingLayoutProvisioner? provisioner = null,
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

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(Now);

        return new ApplyAreaPositionsBatchCommandHandler(
            eventReferenceRepository ?? eventReferences.Object,
            provisioner ?? provisioners.Object,
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            timeProvider.Object,
            NullLogger<ApplyAreaPositionsBatchCommandHandler>.Instance);
    }
}
