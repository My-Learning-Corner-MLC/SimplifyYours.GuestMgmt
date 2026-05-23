using GuestManagementService.Application.Ping;
using Moq;

namespace GuestManagementService.UnitTests.Ping;

public sealed class PingServiceTests
{
    [Fact]
    public void GetStatus_ReturnsServiceUpMessageWithCurrentGmtDateTime()
    {
        var fixedDateTime = new DateTimeOffset(2026, 5, 23, 8, 30, 45, TimeSpan.Zero);
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(fixedDateTime);
        var service = new PingService(timeProvider.Object);

        var response = service.GetStatus();

        Assert.Equal("Guest Management service is up.", response.Message);
        Assert.Equal(fixedDateTime, response.CurrentGmtDateTime);
        Assert.Equal(TimeSpan.Zero, response.CurrentGmtDateTime.Offset);
        timeProvider.Verify(provider => provider.GetUtcNow(), Times.Once);
    }
}
