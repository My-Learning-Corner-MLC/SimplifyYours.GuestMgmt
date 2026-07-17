using FluentValidation;
using GuestManagementService.Application.Guests;
using GuestManagementService.Application.Guests.Birthday;
using GuestManagementService.Application.Guests.Wedding;

namespace GuestManagementService.UnitTests.Guests;

public sealed class GuestMetadataMapperFactoryTests
{
    private readonly GuestMetadataMapperFactory factory = new(
        [new WeddingGuestMetadataMapper(new WeddingGuestMetadataRequestValidator()), new BirthdayGuestMetadataMapper(new BirthdayGuestMetadataRequestValidator())]);

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
    [InlineData("")]
    [InlineData("launch")]
    [InlineData("dinner")]
    public void Resolve_WhenEventTypeIsUnsupported_ThrowsValidationException(string eventType)
    {
        var exception = Assert.Throws<ValidationException>(() => factory.Resolve(eventType));

        Assert.Contains(exception.Errors, error => error.PropertyName == "EventType");
    }
}
