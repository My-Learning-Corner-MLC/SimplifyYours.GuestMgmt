using FluentValidation;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Domain.Invitations;
using MediatR;

namespace GuestManagementService.Application.Invitations.SaveInvitationSettings;

/// <summary>
/// Saves the template and content an organiser composed for an event.
/// </summary>
/// <remarks>
/// Validation lives in <see cref="SaveInvitationSettingsCommandValidator"/> rather than a standalone
/// <c>IValidator{T}</c> because the rules depend on the event's type, which is only known once the
/// event reference has been loaded.
/// <para>
/// Nothing written here reaches event-service. The organiser is editing the invitation's copy of
/// these details, not the event — they may legitimately differ.
/// </para>
/// <para>
/// <b>The template fetch happens here, once, and nowhere else.</b> A <c>templateId</c> is fetched
/// from <c>template-management-service</c>'s current version, validated as parseable Scriban, and
/// snapshotted onto the settings row in the same <see cref="IUnitOfWork.SaveChangesAsync"/> call as
/// the field values. Any failure — unreachable service, unknown template, a template that fails to
/// parse — throws <see cref="ValidationException"/>, which the endpoint turns into a synchronous
/// 400 with nothing persisted. This is deliberately the only call site for
/// <see cref="ITemplateCatalogClient"/> in this service: the anonymous render path must never depend
/// on that service being reachable.
/// </para>
/// </remarks>
public sealed class SaveInvitationSettingsCommandHandler(
    IEventReferenceRepository eventReferenceRepository,
    IEventInvitationSettingsRepository settingsRepository,
    ITemplateCatalogClient templateCatalogClient,
    IInvitationTokenGenerator tokenGenerator,
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
        var failures = SaveInvitationSettingsCommandValidator.Validate(
            request.TemplateId, values, eventType, out var templateId);

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        // Fetch happens once, on this authenticated path, before anything is written. A failure
        // here must reject the whole save — never leave a partially-updated row.
        var fetched = await templateCatalogClient.GetTemplateAsync(templateId!.Value, cancellationToken);

        var snapshotFailures = SaveInvitationSettingsCommandValidator.ValidateFetchedTemplate(fetched, eventType);

        if (snapshotFailures.Count > 0)
        {
            throw new ValidationException(snapshotFailures);
        }

        var template = fetched.Template!;
        var serialized = InvitationFieldValues.Serialize(values, eventType);
        var now = timeProvider.GetUtcNow();

        var existing = await settingsRepository.GetByEventIdAsync(
            request.EventId,
            request.CurrentUser.TenantId,
            cancellationToken);

        EventInvitationSettings settings;

        if (existing is null)
        {
            settings = EventInvitationSettings.Create(
                request.EventId,
                request.CurrentUser.TenantId,
                serialized,
                template.Id,
                template.Name,
                template.Version,
                template.HtmlContent,
                template.CssContent,
                template.JsContent,
                now);

            ApplyPublicLinkChange(settings, request.PublicLinkEnabled, now);

            await settingsRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            existing.UpdateFieldValues(serialized, now);
            existing.SnapshotTemplate(
                template.Id,
                template.Name,
                template.Version,
                template.HtmlContent,
                template.CssContent,
                template.JsContent,
                now);

            ApplyPublicLinkChange(existing, request.PublicLinkEnabled, now);

            settings = existing;
            await settingsRepository.UpdateAsync(existing, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SaveInvitationSettingsResult(
            SaveInvitationSettingsStatus.Saved,
            eventType,
            template.Id.ToString(),
            template.Name,
            InvitationFieldValues.Parse(serialized),
            settings.PublicLinkEnabled,
            settings.PublicEventToken);
    }

    /// <summary>
    /// Folds the public-link enable/disable action into the same save call rather than a separate
    /// endpoint — a dedicated PUT .../public-link route existed at one point and was removed:
    /// composing content and turning on the link it's shared through are the same organiser action.
    /// Omitted (null) is a genuine no-op, not "disable" — leaving the request field out must never
    /// silently turn a live public link off.
    /// </summary>
    private void ApplyPublicLinkChange(EventInvitationSettings settings, bool? enabled, DateTimeOffset now)
    {
        switch (enabled)
        {
            case true:
                settings.EnablePublicLink(tokenGenerator.Generate, now);
                break;
            case false:
                settings.DisablePublicLink(now);
                break;
            case null:
                break;
        }
    }
}
