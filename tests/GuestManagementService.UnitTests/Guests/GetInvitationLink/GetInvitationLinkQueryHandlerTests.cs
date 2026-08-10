using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Guests.GetInvitationLink;
using GuestManagementService.Domain.Guests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Guests.GetInvitationLink;

public sealed class GetInvitationLinkQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("0fa219ed-70ad-4e8d-9f51-6e60409dc659");
    private static readonly Guid GuestId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");

    [Fact]
    public async Task Handle_WhenGuestIsOwned_ReturnsTokenAndUrl()
    {
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.GetByIdAsync(GuestId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewGuest("tok-abc123"));

        var result = await CreateHandler(guests.Object).Handle(Query(), CancellationToken.None);

        Assert.Equal(GetInvitationLinkStatus.Found, result.Status);
        Assert.Equal("tok-abc123", result.InvitationToken);
        Assert.Equal("https://app.example.test/invitation/tok-abc123", result.InvitationUrl);
    }

    [Fact]
    public async Task Handle_WhenGuestBelongsToAnotherTenant_ReturnsNotFound()
    {
        // The repository lookup is tenant-scoped, so another tenant's guest simply is not found.
        // The handler must not distinguish that from a guest that never existed — a 403 here would
        // confirm the guest is real to someone who should not know.
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guest?)null);

        var result = await CreateHandler(guests.Object).Handle(Query(), CancellationToken.None);

        Assert.Equal(GetInvitationLinkStatus.NotFound, result.Status);
        Assert.Null(result.InvitationToken);
        Assert.Null(result.InvitationUrl);
    }

    [Fact]
    public async Task Handle_ScopesTheLookupToTheCallersTenant()
    {
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewGuest("tok-abc123"));

        await CreateHandler(guests.Object).Handle(Query(), CancellationToken.None);

        guests.Verify(
            repository => repository.GetByIdAsync(GuestId, TenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static GetInvitationLinkQuery Query()
    {
        return new GetInvitationLinkQuery(GuestId)
        {
            CurrentUser = new CurrentUser(Guid.NewGuid(), TenantId),
        };
    }

    private static GetInvitationLinkQueryHandler CreateHandler(IGuestRepository guests)
    {
        return new GetInvitationLinkQueryHandler(
            guests,
            new StubLinkBuilder(),
            NullLogger<GetInvitationLinkQueryHandler>.Instance);
    }

    private static Guest NewGuest(string token)
    {
        return Guest.Create(
            GuestId,
            Guid.NewGuid(),
            TenantId,
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            null,
            token,
            new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero));
    }

    private sealed class StubLinkBuilder : IInvitationLinkBuilder
    {
        public string BuildUrl(string invitationToken) =>
            $"https://app.example.test/invitation/{invitationToken}";
    }
}
