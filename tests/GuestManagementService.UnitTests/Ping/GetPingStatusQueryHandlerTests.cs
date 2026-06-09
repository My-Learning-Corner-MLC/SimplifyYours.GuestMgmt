using GuestManagementService.Application.Ping;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Ping;

public sealed class GetPingStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsServiceUpMessageWithCurrentGmtDateTime()
    {
        var fixedDateTime = new DateTimeOffset(2026, 5, 24, 8, 30, 45, TimeSpan.Zero);
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(fixedDateTime);
        var handler = new GetPingStatusQueryHandler(
            timeProvider.Object,
            NullLogger<GetPingStatusQueryHandler>.Instance);

        var response = await handler.Handle(new GetPingStatusQuery(), CancellationToken.None);

        Assert.Equal("Guest Management service is up.", response.Message);
        Assert.Equal(fixedDateTime, response.CurrentGmtDateTime);
        Assert.Equal(TimeSpan.Zero, response.CurrentGmtDateTime.Offset);
        timeProvider.Verify(provider => provider.GetUtcNow(), Times.Once);
    }
}
