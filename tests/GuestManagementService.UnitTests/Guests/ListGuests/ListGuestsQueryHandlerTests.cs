using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Guests.ListGuests;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Guests.ListGuests;

public sealed class ListGuestsQueryHandlerTests
{
    private static readonly Guid TestTenantId = Guid.Parse("c2b9d3a1-4d4b-4b1a-9bc4-2f5a7e8d9f01");
    private static readonly CurrentUser TestUser = new(Guid.NewGuid(), TestTenantId);
    private static readonly DateTimeOffset Now = new(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WhenEventOwned_ReturnsMappedGuests()
    {
        var eventId = Guid.NewGuid();
        var guest = Guest.Create(
            Guid.NewGuid(),
            eventId,
            TestTenantId,
            "Ada",
            "Tester",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            "{\"relationship\":\"Family\",\"side\":\"Bride\",\"plusOnes\":2,\"dietaryNotes\":\"Vegan\"}",
            Now);
        var handler = CreateHandler(eventId, [guest], out _);

        var result = await handler.Handle(Query(eventId), CancellationToken.None);

        Assert.Equal(ListGuestsStatus.Found, result.Status);
        var item = Assert.Single(result.Guests);
        Assert.Equal("Ada", item.FirstName);
        Assert.Equal("Family", item.Relationship);
        Assert.Equal("Bride", item.Side);
        Assert.Equal(2, item.PlusOnes);
        Assert.Equal("Vegan", item.DietaryNotes);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task Handle_WhenNoGuests_ReturnsEmptyList()
    {
        var eventId = Guid.NewGuid();
        var handler = CreateHandler(eventId, [], out _);

        var result = await handler.Handle(Query(eventId), CancellationToken.None);

        Assert.Equal(ListGuestsStatus.Found, result.Status);
        Assert.Empty(result.Guests);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task Handle_PassesPagingSearchAndSortToRepository()
    {
        var eventId = Guid.NewGuid();
        var handler = CreateHandler(eventId, [], out var guests);

        await handler.Handle(
            new ListGuestsQuery(eventId, PageNumber: 2, PageSize: 10, Search: "ada", SortBy: "email", SortDirection: "desc")
            {
                CurrentUser = TestUser
            },
            CancellationToken.None);

        guests.Verify(
            repository => repository.ListAsync(
                It.Is<GuestListQueryOptions>(options =>
                    options.EventId == eventId
                    && options.TenantId == TestTenantId
                    && options.PageNumber == 2
                    && options.PageSize == 10
                    && options.Search == "ada"
                    && options.SortBy == GuestSortField.Email
                    && options.SortDirection == SortDirection.Desc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventMissing_ReturnsEventNotFound()
    {
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventReference?)null);
        var guests = new Mock<IGuestRepository>();
        var handler = new ListGuestsQueryHandler(
            eventReferences.Object,
            guests.Object,
            NullLogger<ListGuestsQueryHandler>.Instance);

        var result = await handler.Handle(Query(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ListGuestsStatus.EventNotFound, result.Status);
        guests.Verify(
            repository => repository.ListAsync(It.IsAny<GuestListQueryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEventBelongsToAnotherTenant_ReturnsEventNotFound()
    {
        var eventId = Guid.NewGuid();
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Wedding", Guid.NewGuid(), Now));
        var guests = new Mock<IGuestRepository>();
        var handler = new ListGuestsQueryHandler(
            eventReferences.Object,
            guests.Object,
            NullLogger<ListGuestsQueryHandler>.Instance);

        var result = await handler.Handle(Query(eventId), CancellationToken.None);

        Assert.Equal(ListGuestsStatus.EventNotFound, result.Status);
    }

    private static ListGuestsQuery Query(Guid eventId)
        => new(eventId) { CurrentUser = TestUser };

    private static ListGuestsQueryHandler CreateHandler(
        Guid eventId,
        IReadOnlyList<Guest> guestList,
        out Mock<IGuestRepository> guests)
    {
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Wedding", TestTenantId, Now));
        guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ListAsync(It.IsAny<GuestListQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuestListQueryOptions options, CancellationToken _) =>
                new GuestListPage(guestList, options.PageNumber, options.PageSize, guestList.Count));

        return new ListGuestsQueryHandler(
            eventReferences.Object,
            guests.Object,
            NullLogger<ListGuestsQueryHandler>.Instance);
    }
}
