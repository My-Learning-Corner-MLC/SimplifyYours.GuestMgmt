using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Seating.UpdateTablePosition;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Seating;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Seating.UpdateTablePosition;

public sealed class UpdateTablePositionCommandHandlerTests
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

        var result = await handler.Handle(Command(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(UpdateTablePositionStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenTableDoesNotExist_ReturnsTableNotFound()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        var handler = CreateHandler(provisioner: provisioner.Object, eventId: eventId);

        var result = await handler.Handle(Command(eventId, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(UpdateTablePositionStatus.TableNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenTableExists_MovesItAndSaves()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var table = layout.AddTable(Guid.NewGuid(), "Family", TableShape.Round, 8, Now);
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var result = await handler.Handle(
            new UpdateTablePositionCommand(eventId, table.Id, 340, 420, 15) { CurrentUser = TestUser },
            CancellationToken.None);

        Assert.Equal(UpdateTablePositionStatus.Updated, result.Status);
        Assert.NotNull(result.Table);
        Assert.Equal(340, result.Table.PositionX);
        Assert.Equal(420, result.Table.PositionY);
        Assert.Equal(15, result.Table.Rotation);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UpdateTablePositionCommand Command(Guid eventId, Guid tableId)
    {
        return new UpdateTablePositionCommand(eventId, tableId, 0, 0, 0) { CurrentUser = TestUser };
    }

    private static UpdateTablePositionCommandHandler CreateHandler(
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

        return new UpdateTablePositionCommandHandler(
            eventReferenceRepository ?? eventReferences.Object,
            provisioner ?? provisioners.Object,
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            timeProvider.Object,
            NullLogger<UpdateTablePositionCommandHandler>.Instance);
    }
}
