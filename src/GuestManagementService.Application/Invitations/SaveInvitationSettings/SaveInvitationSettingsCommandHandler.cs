using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Domain.Invitations;
using MediatR;

namespace GuestManagementService.Application.Invitations.SaveInvitationSettings;

public sealed record SaveInvitationSettingsCommand(
    Guid EventId,
    string? TemplateId,
    IReadOnlyDictionary<string, string?>? FieldValues) : BaseCommand, IRequest<SaveInvitationSettingsResult>;

public enum SaveInvitationSettingsStatus
{
    Saved = 0,
    EventNotFound = 1,
}

public sealed record SaveInvitationSettingsResult(
    SaveInvitationSettingsStatus Status,
    string EventType = "",
    string? TemplateId = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null)
{
    public static SaveInvitationSettingsResult EventNotFound() => new(SaveInvitationSettingsStatus.EventNotFound);
}

/// <summary>
/// Saves the template and content an organiser composed for an event.
/// </summary>
/// <remarks>
/// Validation lives here rather than in a standalone validator because the rules depend on the
/// event's type, which is only known once the event reference has been loaded.
/// <para>
/// Nothing written here reaches event-service. The organiser is editing the invitation's copy of
/// these details, not the event — they may legitimately differ.
/// </para>
/// </remarks>
public sealed class SaveInvitationSettingsCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<SaveInvitationSettingsCommand, SaveInvitationSettingsResult>
{
    public async Task<SaveInvitationSettingsResult> Handle(
        SaveInvitationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var eventReference = await eventReferenceRepository.GetByIdAsync(request.EventId, cancellationToken);

        if (eventReference is null
            || eventReference.IsDeleted
            || eventReference.TenantId != request.CurrentUser.TenantId)
        {
            return SaveInvitationSettingsResult.EventNotFound();
        }

        var eventType = eventReference.EventType;
        InvitationFieldSchema.EnsureSupported(eventType);

        var values = request.FieldValues ?? new Dictionary<string, string?>(StringComparer.Ordinal);
        var failures = Validate(request.TemplateId, values, eventType);

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        var serialized = InvitationFieldValues.Serialize(values, eventType);
        var now = timeProvider.GetUtcNow();

        var existing = await settingsRepository.GetByEventIdAsync(
            request.EventId,
            request.CurrentUser.TenantId,
            cancellationToken);

        if (existing is null)
        {
            await settingsRepository.AddAsync(
                EventInvitationSettings.Create(
                    request.EventId,
                    request.CurrentUser.TenantId,
                    request.TemplateId!,
                    serialized,
                    now),
                cancellationToken);
        }
        else
        {
            existing.Update(request.TemplateId!, serialized, now);
            await settingsRepository.UpdateAsync(existing, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SaveInvitationSettingsResult(
            SaveInvitationSettingsStatus.Saved,
            eventType,
            request.TemplateId,
            InvitationFieldValues.Parse(serialized));
    }

    internal static List<ValidationFailure> Validate(
        string? templateId,
        IReadOnlyDictionary<string, string?> values,
        string eventType)
    {
        var failures = new List<ValidationFailure>();

        if (string.IsNullOrWhiteSpace(templateId))
        {
            failures.Add(new ValidationFailure(nameof(SaveInvitationSettingsCommand.TemplateId),
                "Choose a template."));
        }

        var allowed = InvitationFieldSchema.AllFor(eventType).ToHashSet(StringComparer.Ordinal);

        // Unknown keys are rejected rather than dropped. Silently ignoring them would let an
        // organiser fill in a field, save successfully, and never see it on the invitation.
        foreach (var key in values.Keys.Where(key => !allowed.Contains(key)))
        {
            failures.Add(new ValidationFailure(key,
                $"'{key}' is not a field on a {eventType} invitation."));
        }

        foreach (var field in InvitationFieldSchema.RequiredFor(eventType))
        {
            if (!values.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
            {
                failures.Add(new ValidationFailure(field, "This field is required."));
            }
        }

        foreach (var (field, value) in values)
        {
            var max = InvitationFieldSchema.MaxLengthOf(field);

            if (value is not null && value.Trim().Length > max)
            {
                failures.Add(new ValidationFailure(field, $"Must be {max} characters or fewer."));
            }
        }

        return failures;
    }
}
