using FluentValidation;
using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Domain.Invitations;
using MediatR;
using Scriban;

namespace GuestManagementService.Application.Invitations.SaveInvitationSettings;

/// <param name="PublicLinkEnabled">
/// Omitted (null) leaves the public link untouched. <see langword="true"/> enables it, minting a
/// token if none exists. <see langword="false"/> disables it AND revokes the token — see
/// <c>EventInvitationSettings.DisablePublicLink</c>.
/// </param>
public sealed record SaveInvitationSettingsCommand(
    Guid EventId,
    string? TemplateId,
    IReadOnlyDictionary<string, string?>? FieldValues,
    bool? PublicLinkEnabled = null) : BaseCommand, IRequest<SaveInvitationSettingsResult>;

public enum SaveInvitationSettingsStatus
{
    Saved = 0,
    EventNotFound = 1,
}

public sealed record SaveInvitationSettingsResult(
    SaveInvitationSettingsStatus Status,
    string EventType = "",
    string? TemplateId = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null,
    bool PublicLinkEnabled = false,
    string? PublicEventToken = null)
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
        var failures = Validate(request.TemplateId, values, eventType, out var templateId);

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        // Fetch happens once, on this authenticated path, before anything is written. A failure
        // here must reject the whole save — never leave a partially-updated row.
        var fetched = await templateCatalogClient.GetTemplateAsync(templateId!.Value, cancellationToken);

        var snapshotFailures = ValidateFetchedTemplate(fetched, eventType);

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

    internal static List<ValidationFailure> Validate(
        string? templateId,
        IReadOnlyDictionary<string, string?> values,
        string eventType,
        out Guid? parsedTemplateId)
    {
        var failures = new List<ValidationFailure>();
        parsedTemplateId = null;

        if (string.IsNullOrWhiteSpace(templateId))
        {
            failures.Add(new ValidationFailure(nameof(SaveInvitationSettingsCommand.TemplateId),
                "Choose a template."));
        }
        else if (!Guid.TryParse(templateId, out var parsed) || parsed == Guid.Empty)
        {
            failures.Add(new ValidationFailure(nameof(SaveInvitationSettingsCommand.TemplateId),
                "That template id is not valid."));
        }
        else
        {
            parsedTemplateId = parsed;
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

    /// <summary>
    /// Turns every snapshot failure mode — unreachable, not found, or a template whose html fails
    /// to parse as Scriban — into the same field-keyed validation failure, so the endpoint's
    /// existing <c>catch (ValidationException)</c> path produces a consistent 400 for all of them.
    /// </summary>
    internal static List<ValidationFailure> ValidateFetchedTemplate(TemplateFetchResult fetched, string eventType)
    {
        const string field = nameof(SaveInvitationSettingsCommand.TemplateId);

        switch (fetched.Status)
        {
            case TemplateFetchStatus.NotFound:
                return [new ValidationFailure(field, "That template could not be found.")];

            case TemplateFetchStatus.Unavailable:
                return
                [
                    new ValidationFailure(
                        field,
                        "The template catalog could not be reached. Please try again."),
                ];

            case TemplateFetchStatus.Found:
            default:
                break;
        }

        var template = fetched.Template!;
        var parsed = Template.Parse(template.HtmlContent);

        if (parsed.HasErrors)
        {
            return
            [
                new ValidationFailure(
                    field,
                    "That template failed to parse and cannot be used: "
                    + string.Join("; ", parsed.Messages)),
            ];
        }

        return [];
    }
}
