using GuestManagementService.Application.Guests;
using GuestManagementService.Application.Guests.Birthday;
using GuestManagementService.Application.Guests.Wedding;

namespace GuestManagementService.UnitTests.Guests;

public sealed class GuestMetadataMapperFactoryTests
{
    private readonly GuestMetadataMapperFactory factory = new(
        [new WeddingGuestMetadataMapper(), new BirthdayGuestMetadataMapper()]);

    [Theory]
    [InlineData("wedding", typeof(WeddingGuestMetadataMapper))]
    [InlineData("Wedding", typeof(WeddingGuestMetadataMapper))]
    [InlineData("birthday", typeof(BirthdayGuestMetadataMapper))]
    public void Resolve_WhenEventTypeIsKnown_ReturnsMatchingMapper(string eventType, Type expectedMapperType)
    {
        var mapper = factory.Resolve(eventType);

        Assert.NotNull(mapper);
        Assert.IsType(expectedMapperType, mapper);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("launch")]
    public void Resolve_WhenEventTypeIsUnknownOrEmpty_ReturnsNull(string? eventType)
    {
        Assert.Null(factory.Resolve(eventType));
    }
}
