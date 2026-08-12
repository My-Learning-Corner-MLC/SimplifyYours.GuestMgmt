namespace GuestManagementService.Domain.Invitations;

/// <summary>
/// The invitation an organiser has composed for one event: which template, and the content that
/// fills it.
/// </summary>
/// <remarks>
/// Deliberately separate from <c>EventReference</c>. That is a replicated read model of data owned
/// by event-service; this is content the organiser authored here. Merging them would make "what
/// the event says" and "what the invitation says" indistinguishable — and the two are allowed to
/// differ, because an organiser may want a friendly venue line on the invitation and a precise
/// postal address on the event.
/// <para>
/// <c>FieldValues</c> is opaque JSON rather than columns: the field set depends on the event type
/// and on the chosen template, and will grow as the gallery does. Storing it as jsonb keeps it
/// queryable without a migration per template.
/// </para>
/// </remarks>
public sealed class EventInvitationSettings
{
    private EventInvitationSettings()
    {
    }

    private EventInvitationSettings(
        Guid eventId,
        Guid tenantId,
        string templateId,
        string fieldValues,
        DateTimeOffset createdAt)
    {
        EventId = eventId;
        TenantId = tenantId;
        TemplateId = NormalizeRequired(templateId, nameof(templateId));
        FieldValues = NormalizeRequired(fieldValues, nameof(fieldValues));
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = createdAt.ToUniversalTime();
    }

    /// <summary>One composed invitation per event, so the event id is the key.</summary>
    public Guid EventId { get; private set; }

    public Guid TenantId { get; private set; }

    public string TemplateId { get; private set; } = string.Empty;

    /// <summary>JSON object of merge-token values the organiser typed or edited.</summary>
    public string FieldValues { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static EventInvitationSettings Create(
        Guid eventId,
        Guid tenantId,
        string templateId,
        string fieldValues,
        DateTimeOffset createdAt)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id must not be empty.", nameof(eventId));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        return new EventInvitationSettings(eventId, tenantId, templateId, fieldValues, createdAt);
    }

    /// <summary>
    /// Replaces the template and its content. Every already-issued invitation link renders at view
    /// time, so this takes effect immediately for guests who have not yet opened theirs — and for
    /// those who have.
    /// </summary>
    public void Update(string templateId, string fieldValues, DateTimeOffset updatedAt)
    {
        TemplateId = NormalizeRequired(templateId, nameof(templateId));
        FieldValues = NormalizeRequired(fieldValues, nameof(fieldValues));
        UpdatedAt = updatedAt.ToUniversalTime();
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}
