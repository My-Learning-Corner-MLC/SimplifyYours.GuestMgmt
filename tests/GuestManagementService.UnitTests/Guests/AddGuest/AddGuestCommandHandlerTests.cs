using System.Text.Json;
using FluentValidation;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Guests;
using GuestManagementService.Application.Guests.AddGuest;
using GuestManagementService.Application.Guests.Wedding;
using GuestManagementService.Contracts.Guests.Wedding;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Guests.AddGuest;

public sealed class AddGuestCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("ff3d23f3-6a5e-4555-b189-630dfd24bad8");
    private static readonly Guid TestTenantId = Guid.Parse("c2b9d3a1-4d4b-4b1a-9bc4-2f5a7e8d9f01");
    private static readonly CurrentUser TestUser = new(TestUserId, TestTenantId);
    private static readonly IGuestMetadataMapperFactory MetadataMapperFactory =
        new GuestMetadataMapperFactory([new WeddingGuestMetadataMapper()]);

    [Fact]
    public async Task Handle_WhenEventExists_CreatesGuest()
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        Guest? savedGuest = null;
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Launch", TestTenantId, now, "launch"));
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()))
            .Callback<Guest, CancellationToken>((guest, _) => savedGuest = guest)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);
        var handler = new AddGuestCommandHandler(
            eventReferences.Object,
            guests.Object,
            MetadataMapperFactory,
            unitOfWork.Object,
            timeProvider.Object,
            NullLogger<AddGuestCommandHandler>.Instance);

        var result = await handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Tester",
            "+1 555 123 4567",
            "test@example.com",
            null)
        {
            CurrentUser = TestUser
        },
            CancellationToken.None);

        Assert.Equal(AddGuestStatus.Created, result.Status);
        Assert.NotNull(result.Guest);
        Assert.Equal(eventId, result.Guest.EventId);
        Assert.Equal("preferNotToSay", result.Guest.Gender);
        Assert.Equal("test@example.com", result.Guest.EmailAddress);
        Assert.Null(result.Guest.EventMetadata);
        Assert.NotNull(savedGuest);
        Assert.Equal(TestTenantId, savedGuest.TenantId);
        Assert.Equal("+15551234567", savedGuest.NormalizedPhoneNumber);
        Assert.Equal("test@example.com", savedGuest.NormalizedEmailAddress);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWeddingFieldsProvidedForWeddingEvent_PersistsMetadataAndReturnsThem()
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        Guest? savedGuest = null;
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Whitmore Wedding", TestTenantId, now, "wedding"));
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()))
            .Callback<Guest, CancellationToken>((guest, _) => savedGuest = guest)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);
        var handler = new AddGuestCommandHandler(
            eventReferences.Object,
            guests.Object,
            MetadataMapperFactory,
            unitOfWork.Object,
            timeProvider.Object,
            NullLogger<AddGuestCommandHandler>.Instance);

        var result = await handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Tester",
            "+1 555 123 4567",
            "test@example.com",
            null,
            WeddingMetadata("Family", "Bride", 2, "Pescatarian"))
        {
            CurrentUser = TestUser
        },
            CancellationToken.None);

        Assert.Equal(AddGuestStatus.Created, result.Status);
        Assert.NotNull(result.Guest);
        var metadata = Assert.IsType<WeddingGuestMetadataResponse>(result.Guest.EventMetadata);
        Assert.Equal("Family", metadata.Relationship);
        Assert.Equal("Bride", metadata.Side);
        Assert.Equal(2, metadata.PlusOnes);
        Assert.Equal("Pescatarian", metadata.DietaryNotes);
        Assert.NotNull(savedGuest);
        Assert.NotNull(savedGuest.Metadata);
        Assert.Contains("\"relationship\":\"Family\"", savedGuest.Metadata);
        Assert.Contains("\"side\":\"Bride\"", savedGuest.Metadata);
    }

    [Fact]
    public async Task Handle_WhenWeddingFieldsProvidedForNonWeddingEvent_IgnoresMetadata()
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        Guest? savedGuest = null;
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Launch", TestTenantId, now, "launch"));
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()))
            .Callback<Guest, CancellationToken>((guest, _) => savedGuest = guest)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);
        var handler = new AddGuestCommandHandler(
            eventReferences.Object,
            guests.Object,
            MetadataMapperFactory,
            unitOfWork.Object,
            timeProvider.Object,
            NullLogger<AddGuestCommandHandler>.Instance);

        var result = await handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Tester",
            "+1 555 123 4567",
            "test@example.com",
            null,
            WeddingMetadata("Family", "Bride", 2, "Pescatarian"))
        {
            CurrentUser = TestUser
        },
            CancellationToken.None);

        Assert.Equal(AddGuestStatus.Created, result.Status);
        Assert.NotNull(result.Guest);
        Assert.Null(result.Guest.EventMetadata);
        Assert.NotNull(savedGuest);
        Assert.Null(savedGuest.Metadata);
    }

    [Fact]
    public async Task Handle_WhenWeddingMetadataIsInvalid_ThrowsValidationException()
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Whitmore Wedding", TestTenantId, now, "wedding"));
        var guests = new Mock<IGuestRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);
        var handler = new AddGuestCommandHandler(
            eventReferences.Object,
            guests.Object,
            MetadataMapperFactory,
            unitOfWork.Object,
            timeProvider.Object,
            NullLogger<AddGuestCommandHandler>.Instance);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Tester",
            "+1 555 123 4567",
            "test@example.com",
            null,
            WeddingMetadata("Cousin", "Bride", 2, null))
        {
            CurrentUser = TestUser
        },
            CancellationToken.None));

        guests.Verify(
            repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEventIsMissing_ReturnsEventNotFound()
    {
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventReference?)null);
        var handler = CreateHandler(eventReferences.Object);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AddGuestStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenPhoneIsDuplicate_ReturnsDuplicate()
    {
        var eventId = Guid.NewGuid();
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ExistsByPhoneAsync(eventId, "+15551234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler(guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(ValidCommand(eventId), CancellationToken.None);

        Assert.Equal(AddGuestStatus.Duplicate, result.Status);
    }

    [Fact]
    public async Task Handle_WhenProvidedEmailIsDuplicate_ReturnsDuplicate()
    {
        var eventId = Guid.NewGuid();
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.ExistsByEmailAsync(eventId, "test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler(guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(ValidCommand(eventId), CancellationToken.None);

        Assert.Equal(AddGuestStatus.Duplicate, result.Status);
    }

    [Fact]
    public async Task Handle_WhenEmailIsOmitted_DoesNotCheckDuplicateEmail()
    {
        var eventId = Guid.NewGuid();
        var guests = new Mock<IGuestRepository>();
        var handler = CreateHandler(guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Tester",
            "+1 555 123 4567",
            null,
            "female")
        {
            CurrentUser = TestUser
        },
            CancellationToken.None);

        Assert.Equal(AddGuestStatus.Created, result.Status);
        guests.Verify(
            repository => repository.ExistsByEmailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEventIsDeleted_ReturnsEventNotFound()
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        var deletedReference = EventReference.Active(eventId, "Launch", TestTenantId, now);
        deletedReference.MarkDeleted(now);
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedReference);
        var guests = new Mock<IGuestRepository>();
        var handler = CreateHandler(eventReferences.Object, guests.Object);

        var result = await handler.Handle(ValidCommand(eventId), CancellationToken.None);

        Assert.Equal(AddGuestStatus.EventNotFound, result.Status);
        guests.Verify(
            repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGenderIsValidMale_StoresMaleGender()
    {
        var eventId = Guid.NewGuid();
        Guest? savedGuest = null;
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()))
            .Callback<Guest, CancellationToken>((guest, _) => savedGuest = guest)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Tester",
            "+1 555 123 4567",
            null,
            "male")
        {
            CurrentUser = TestUser
        },
            CancellationToken.None);

        Assert.Equal(AddGuestStatus.Created, result.Status);
        Assert.NotNull(result.Guest);
        Assert.Equal("male", result.Guest.Gender);
        Assert.NotNull(savedGuest);
    }

    [Fact]
    public async Task Handle_WhenGenderIsUnrecognizedString_DefaultsToPreferNotToSay()
    {
        var eventId = Guid.NewGuid();
        Guest? savedGuest = null;
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()))
            .Callback<Guest, CancellationToken>((guest, _) => savedGuest = guest)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(guestRepository: guests.Object, eventId: eventId);

        var result = await handler.Handle(new AddGuestCommand(
            eventId,
            "Ada",
            "Tester",
            "+1 555 123 4567",
            null,
            "unknown-value")
        {
            CurrentUser = TestUser
        },
            CancellationToken.None);

        Assert.Equal(AddGuestStatus.Created, result.Status);
        Assert.NotNull(result.Guest);
        Assert.Equal("preferNotToSay", result.Guest.Gender);
        Assert.NotNull(savedGuest);
    }

    [Fact]
    public async Task Handle_WhenEventBelongsToAnotherTenant_ReturnsEventNotFound()
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(eventId, "Launch", Guid.NewGuid(), now));
        var guests = new Mock<IGuestRepository>();
        var handler = CreateHandler(eventReferences.Object, guests.Object);

        var result = await handler.Handle(ValidCommand(eventId), CancellationToken.None);

        Assert.Equal(AddGuestStatus.EventNotFound, result.Status);
        guests.Verify(
            repository => repository.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static JsonElement WeddingMetadata(
        string? relationship,
        string? side,
        int? plusOnes,
        string? dietaryNotes)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            relationship,
            side,
            plusOnes,
            dietaryNotes
        }));
        return document.RootElement.Clone();
    }

    private static AddGuestCommand ValidCommand(Guid eventId)
    {
        return new AddGuestCommand(
            eventId,
            "Ada",
            "Tester",
            "+1 555 123 4567",
            "test@example.com",
            "female")
        {
            CurrentUser = TestUser
        };
    }

    private static AddGuestCommandHandler CreateHandler(
        IEventReferenceRepository? eventReferenceRepository = null,
        IGuestRepository? guestRepository = null,
        Guid? eventId = null)
    {
        var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);
        var resolvedEventId = eventId ?? Guid.NewGuid();
        var eventReferences = new Mock<IEventReferenceRepository>();
        eventReferences
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EventReference.Active(resolvedEventId, "Launch", TestTenantId, now));
        var guests = new Mock<IGuestRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);

        return new AddGuestCommandHandler(
            eventReferenceRepository ?? eventReferences.Object,
            guestRepository ?? guests.Object,
            MetadataMapperFactory,
            unitOfWork.Object,
            timeProvider.Object,
            NullLogger<AddGuestCommandHandler>.Instance);
    }
}
