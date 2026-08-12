using FluentValidation;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Invitations;
using GuestManagementService.Application.Invitations.SubmitRsvp;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GuestManagementService.UnitTests.Invitations;

public sealed class SubmitRsvpCommandHandlerTests
{
    private const string Token = "tok-abc123";
    private static readonly Guid EventId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");
    private static readonly DateTimeOffset BeforeDeadline = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AfterDeadline = new(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_RecordsTheGuestsAnswer()
    {
        var guest = NewGuest("{\"plusOnes\":2}");

        var result = await Handle(guest, Command("Accepted", 1, "Pescatarian"));

        Assert.Equal(SubmitRsvpStatus.Accepted, result.Status);
        Assert.Equal(RsvpStatus.Accepted, guest.RsvpStatus);
        Assert.Equal(1, InvitationMetadata.ReadPlusOnesConfirmed(guest.Metadata));
        Assert.Equal("Pescatarian", InvitationMetadata.ReadDietaryNotes(guest.Metadata));
    }

    [Fact]
    public async Task Handle_NeverOverwritesTheOrganisersPlusOnesAllowance()
    {
        // The whole point of keeping the two counts in separate fields. If a guest's answer could
        // move the allowance, an anonymous caller would be editing the organiser's own data.
        var guest = NewGuest("{\"plusOnes\":2,\"relationship\":\"Family\"}");

        await Handle(guest, Command("Accepted", 2, null));

        Assert.Equal(2, InvitationMetadata.ReadPlusOnesAllowed(guest.Metadata));
        Assert.Contains("\"relationship\":\"Family\"", guest.Metadata, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_RejectsMorePlusOnesThanTheOrganiserAllowed()
    {
        var guest = NewGuest("{\"plusOnes\":2}");

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => Handle(guest, Command("Accepted", 3, null)));

        Assert.Contains("up to 2 guests", exception.Message, StringComparison.Ordinal);
        Assert.Equal(RsvpStatus.NoResponse, guest.RsvpStatus);
    }

    [Fact]
    public async Task Handle_RejectsAnyPlusOnesWhenTheInvitationIncludesNone()
    {
        var guest = NewGuest("{}");

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => Handle(guest, Command("Accepted", 1, null)));

        Assert.Contains("does not include additional guests", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_AfterTheDeadline_RejectsAndLeavesTheAnswerUntouched()
    {
        var guest = NewGuest("{\"plusOnes\":2}");

        var result = await Handle(guest, Command("Accepted", 1, null), now: AfterDeadline);

        Assert.Equal(SubmitRsvpStatus.Closed, result.Status);
        Assert.Equal(RsvpStatus.NoResponse, guest.RsvpStatus);
    }

    [Fact]
    public async Task Handle_AfterTheDeadline_RejectsEditsToAnExistingAnswerToo()
    {
        // The deadline exists so catering numbers stop moving; an edit window past it would defeat
        // it just as thoroughly as a late first response.
        var guest = NewGuest("{\"plusOnes\":2}");
        guest.RecordRsvp(RsvpStatus.Accepted, guest.Metadata, BeforeDeadline);

        var result = await Handle(guest, Command("Declined", 0, null), now: AfterDeadline);

        Assert.Equal(SubmitRsvpStatus.Closed, result.Status);
        Assert.Equal(RsvpStatus.Accepted, guest.RsvpStatus);
    }

    [Fact]
    public async Task Handle_EditingAnAnswer_KeepsTheOriginalRespondedAt()
    {
        var guest = NewGuest("{\"plusOnes\":2}");
        var firstResponse = BeforeDeadline;
        guest.RecordRsvp(RsvpStatus.Accepted, guest.Metadata, firstResponse);

        await Handle(guest, Command("Declined", 0, null), now: firstResponse.AddDays(3));

        Assert.Equal(RsvpStatus.Declined, guest.RsvpStatus);
        Assert.Equal(firstResponse, guest.RespondedAt);
    }

    [Fact]
    public async Task Handle_WhenTokenIsUnknown_ReturnsNotFoundWithoutWriting()
    {
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(r => r.GetByInvitationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guest?)null);
        var unitOfWork = new Mock<IUnitOfWork>();

        var result = await CreateHandler(guests.Object, NewEvent(), BeforeDeadline, unitOfWork.Object)
            .Handle(Command("Accepted", 0, null), CancellationToken.None);

        Assert.Equal(SubmitRsvpStatus.NotFound, result.Status);
        unitOfWork.Verify(w => w.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheEventIsDeleted_ReturnsNotFound()
    {
        var deleted = NewEvent();
        deleted.MarkDeleted(BeforeDeadline);

        var result = await Handle(NewGuest("{}"), Command("Accepted", 0, null), eventReference: deleted);

        Assert.Equal(SubmitRsvpStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_AcceptsAMaybeWithoutAGuestCount()
    {
        // The RSVP design shows no stepper on Maybe — nothing to plan for yet.
        var guest = NewGuest("{\"plusOnes\":2}");

        var result = await Handle(guest, Command("Maybe", 0, "Waiting on a flight schedule"));

        Assert.Equal(SubmitRsvpStatus.Accepted, result.Status);
        Assert.Equal(RsvpStatus.Maybe, guest.RsvpStatus);
        Assert.Equal("Waiting on a flight schedule", InvitationMetadata.ReadDietaryNotes(guest.Metadata));
    }

    [Fact]
    public async Task Handle_PersistsThroughTheUnitOfWork()
    {
        var guest = NewGuest("{\"plusOnes\":1}");
        var unitOfWork = new Mock<IUnitOfWork>();

        await Handle(guest, Command("Maybe", 0, null), unitOfWork: unitOfWork.Object);

        unitOfWork.Verify(w => w.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SubmitRsvpCommand Command(string status, int? plusOnes, string? notes) =>
        new(Token, status, plusOnes, notes);

    private static async Task<SubmitRsvpResult> Handle(
        Guest guest,
        SubmitRsvpCommand command,
        DateTimeOffset? now = null,
        EventReference? eventReference = null,
        IUnitOfWork? unitOfWork = null)
    {
        var guests = new Mock<IGuestRepository>();
        guests
            .Setup(r => r.GetByInvitationTokenAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guest);

        return await CreateHandler(
                guests.Object,
                eventReference ?? NewEvent(),
                now ?? BeforeDeadline,
                unitOfWork ?? new Mock<IUnitOfWork>().Object)
            .Handle(command, CancellationToken.None);
    }

    private static SubmitRsvpCommandHandler CreateHandler(
        IGuestRepository guests,
        EventReference? eventReference,
        DateTimeOffset now,
        IUnitOfWork unitOfWork)
    {
        var events = new Mock<IEventReferenceRepository>();
        events
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventReference);

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);

        return new SubmitRsvpCommandHandler(
            guests,
            events.Object,
            unitOfWork,
            timeProvider.Object,
            NullLogger<SubmitRsvpCommandHandler>.Instance);
    }

    private static Guest NewGuest(string? metadata)
    {
        return Guest.Create(
            Guid.NewGuid(),
            EventId,
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            "+15551234567",
            "+15551234567",
            "ada@example.com",
            "ada@example.com",
            Gender.PreferNotToSay,
            metadata,
            Token,
            BeforeDeadline);
    }

    private static EventReference NewEvent()
    {
        return EventReference.Active(
            EventId,
            "Amara & Julian",
            Guid.NewGuid(),
            BeforeDeadline,
            "wedding",
            new DateOnly(2026, 9, 12),
            new TimeOnly(18, 30),
            "Asia/Ho_Chi_Minh",
            "Villa Astoria",
            "Lake Como",
            null);
    }
}
