using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Guests;
using GuestManagementService.Domain.Guests;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Application.Guests.AddGuest;

public sealed class AddGuestCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IGuestRepository guestRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AddGuestCommandHandler> logger)
    : IRequestHandler<AddGuestCommand, AddGuestResult>
{
    public async Task<AddGuestResult> Handle(AddGuestCommand request, CancellationToken cancellationToken)
    {
        var currentUser = request.CurrentUser;

        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null || eventReference.IsDeleted)
        {
            logger.LogWarning(
                "Guest add requested but event reference was not available. EventId: {EventId}.",
                request.EventId);
            return AddGuestResult.EventNotFound();
        }

        if (eventReference.TenantId != currentUser.TenantId)
        {
            logger.LogWarning(
                "Guest add requested but event reference is owned by another tenant. EventId: {EventId}.",
                request.EventId);
            return AddGuestResult.EventNotFound();
        }

        var normalizedPhone = GuestNormalization.NormalizePhone(request.PhoneNumber ?? string.Empty);
        var normalizedEmail = GuestNormalization.NormalizeEmail(request.EmailAddress);

        if (await guestRepository.ExistsByPhoneAsync(request.EventId, normalizedPhone, cancellationToken))
        {
            logger.LogWarning(
                "Guest add rejected because phone number already exists for event. EventId: {EventId}.",
                request.EventId);
            return AddGuestResult.Duplicate();
        }

        if (normalizedEmail is not null
            && await guestRepository.ExistsByEmailAsync(request.EventId, normalizedEmail, cancellationToken))
        {
            logger.LogWarning(
                "Guest add rejected because email address already exists for event. EventId: {EventId}.",
                request.EventId);
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
            currentUser.TenantId,
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

        logger.LogInformation(
            "Guest added. GuestId: {GuestId}. EventId: {EventId}.",
            guest.Id,
            guest.EventId);

        return AddGuestResult.Created(GuestDetails.From(guest));
    }
}
