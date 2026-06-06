using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Guests;
using GuestManagementService.Domain.Guests;
using MediatR;

namespace GuestManagementService.Application.Guests.AddGuest;

public sealed class AddGuestCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IGuestRepository guestRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<AddGuestCommand, AddGuestResult>
{
    public async Task<AddGuestResult> Handle(AddGuestCommand request, CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            return AddGuestResult.EventNotFound();
        }

        var normalizedPhone = GuestNormalization.NormalizePhone(request.PhoneNumber ?? string.Empty);
        var normalizedEmail = GuestNormalization.NormalizeEmail(request.EmailAddress);

        if (await guestRepository.ExistsByPhoneAsync(request.EventId, normalizedPhone, cancellationToken))
        {
            return AddGuestResult.Duplicate();
        }

        if (normalizedEmail is not null
            && await guestRepository.ExistsByEmailAsync(request.EventId, normalizedEmail, cancellationToken))
        {
            return AddGuestResult.Duplicate();
        }

        if (!GuestParsing.TryParseGender(request.Gender, out var gender))
        {
            gender = Gender.PreferNotToSay;
        }

        var now = timeProvider.GetUtcNow();
        var guest = Guest.Create(
            Guid.NewGuid(),
            request.EventId,
            request.FirstName ?? string.Empty,
            request.LastName ?? string.Empty,
            request.PhoneNumber ?? string.Empty,
            normalizedPhone,
            request.EmailAddress,
            normalizedEmail,
            gender,
            now);

        await guestRepository.AddAsync(guest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AddGuestResult.Created(GuestDetails.From(guest));
    }
}
