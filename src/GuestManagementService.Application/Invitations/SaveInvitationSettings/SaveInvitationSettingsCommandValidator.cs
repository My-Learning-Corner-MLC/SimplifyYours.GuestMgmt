using FluentValidation.Results;
using GuestManagementService.Application.Abstractions.Invitations;

namespace GuestManagementService.Application.Invitations.SaveInvitationSettings;

/// <summary>
/// Not a standard FluentValidation <c>IValidator{T}</c> — the rules depend on the event's type,
/// which is only known once <see cref="SaveInvitationSettingsCommandHandler"/> has loaded the event
/// reference, so these run as plain static helpers from inside the handler instead.
/// </summary>
internal static class SaveInvitationSettingsCommandValidator
{
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
        var parsed = Scriban.Template.Parse(template.HtmlContent);

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
