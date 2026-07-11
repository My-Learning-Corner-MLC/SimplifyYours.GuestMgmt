using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Seating;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Seating;
using GuestManagementService.Application.Seating.CreateTables;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Seating;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Seating.CreateTables;

public sealed class CreateTablesCommandHandlerTests
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

        var result = await handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(CreateTablesStatus.EventNotFound, result.Status);
        Assert.Empty(result.Tables);
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

        var result = await handler.Handle(Command(eventId), CancellationToken.None);

        Assert.Equal(CreateTablesStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenCountIsOne_CreatesSingleTableWithRequestedName()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = CreateHandler(provisioner: provisioner.Object, unitOfWork: unitOfWork.Object, eventId: eventId);

        var result = await handler.Handle(
            new CreateTablesCommand(eventId, "Family", "Round", 8, 1) { CurrentUser = TestUser },
            CancellationToken.None);

        Assert.Equal(CreateTablesStatus.Created, result.Status);
        var table = Assert.Single(result.Tables);
        Assert.Equal("Family", table.Name);
        Assert.Equal("Round", table.Shape);
        Assert.Equal(8, table.SeatCount);
        Assert.Single(layout.Tables);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCountIsGreaterThanOne_CreatesNumberedTables()
    {
        var eventId = Guid.NewGuid();
        var layout = SeatingLayout.Create(Guid.NewGuid(), eventId, TestTenantId, Now);
        var provisioner = new Mock<ISeatingLayoutProvisioner>();
        provisioner
            .Setup(p => p.GetOrCreateAsync(eventId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        var handler = CreateHandler(provisioner: provisioner.Object, eventId: eventId);

        var result = await handler.Handle(
            new CreateTablesCommand(eventId, "Family", "Round", 8, 3) { CurrentUser = TestUser },
            CancellationToken.None);

        Assert.Equal(CreateTablesStatus.Created, result.Status);
        Assert.Equal(3, result.Tables.Count);
        Assert.Equal(["Family · 1", "Family · 2", "Family · 3"], result.Tables.Select(t => t.Name));
        Assert.Equal(3, layout.Tables.Count);
    }

    private static CreateTablesCommand Command(Guid eventId)
    {
        return new CreateTablesCommand(eventId, "Family", "Round", 8, 1) { CurrentUser = TestUser };
    }

    private static CreateTablesCommandHandler CreateHandler(
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

        return new CreateTablesCommandHandler(
            eventReferenceRepository ?? eventReferences.Object,
            provisioner ?? provisioners.Object,
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            timeProvider.Object,
            NullLogger<CreateTablesCommandHandler>.Instance);
    }
}
