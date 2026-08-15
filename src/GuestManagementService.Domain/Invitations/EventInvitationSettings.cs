namespace GuestManagementService.Domain.Invitations;

/// <summary>
/// The invitation an organiser has composed for one event: which template (snapshotted from
/// <c>template-management-service</c> at the moment it was chosen), the content that fills it, and
/// the two anonymous surfaces derived from it — the public event link and the organiser's preview.
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
/// <para>
/// <b>The template snapshot is the whole point of slice 2.</b> <c>TemplateId</c>/<c>TemplateVersion</c>
/// identify which catalog template was chosen and which version was fetched; <c>HtmlContent</c>,
/// <c>CssContent</c> and <c>JsContent</c> are that version's content, copied here once on save so the
/// anonymous render path never depends on <c>template-management-service</c> being reachable. A row
/// can exist with no snapshot — a slice-1 row whose test template id did not survive the B1 migration
/// — and the render path must treat that the same as "not configured yet", never as a broken page.
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
        string fieldValues,
        DateTimeOffset createdAt)
    {
        EventId = eventId;
        TenantId = tenantId;
        FieldValues = NormalizeRequired(fieldValues, nameof(fieldValues));
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = createdAt.ToUniversalTime();
    }

    /// <summary>One composed invitation per event, so the event id is the key.</summary>
    public Guid EventId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>
    /// The catalog template chosen, or <see langword="null"/> when no valid snapshot exists — either
    /// nothing has been chosen yet, or (transitionally) a slice-1 row whose test template id did not
    /// parse as a <see cref="Guid"/> during the B1 migration.
    /// </summary>
    public Guid? TemplateId { get; private set; }

    /// <summary>The catalog version fetched at snapshot time. Immutable once stored.</summary>
    public int? TemplateVersion { get; private set; }

    public string? HtmlContent { get; private set; }

    public string? CssContent { get; private set; }

    public string? JsContent { get; private set; }

    /// <summary>JSON object of merge-token values the organiser typed or edited.</summary>
    public string FieldValues { get; private set; } = string.Empty;

    /// <summary>Default off. Enabling mints <see cref="PublicEventToken"/> if one does not exist yet.</summary>
    public bool PublicLinkEnabled { get; private set; }

    /// <summary>
    /// Identifies the event, never a person. <see langword="null"/> until first enabled. Rotating
    /// (revoke) overwrites this so the previous URL stops resolving.
    /// </summary>
    public string? PublicEventToken { get; private set; }

    /// <summary>
    /// Short-lived, organiser-only credential that lets an <c>&lt;iframe&gt;</c> (which cannot carry
    /// an <c>Authorization</c> header) reach the render path. Re-issuing overwrites the previous one.
    /// </summary>
    public string? PreviewToken { get; private set; }

    public DateTimeOffset? PreviewExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Creates the row and its first template snapshot in one step — a settings row without a
    /// snapshot is not a state the save path can produce; it only arises from historical data.
    /// </summary>
    public static EventInvitationSettings Create(
        Guid eventId,
        Guid tenantId,
        string fieldValues,
        Guid templateId,
        int templateVersion,
        string htmlContent,
        string? cssContent,
        string? jsContent,
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

        var settings = new EventInvitationSettings(eventId, tenantId, fieldValues, createdAt);
        settings.SnapshotTemplate(templateId, templateVersion, htmlContent, cssContent, jsContent, createdAt);

        return settings;
    }

    /// <summary>
    /// Replaces the organiser-authored content. Every already-issued invitation link renders at view
    /// time, so this takes effect immediately for guests who have not yet opened theirs — and for
    /// those who have.
    /// </summary>
    public void UpdateFieldValues(string fieldValues, DateTimeOffset updatedAt)
    {
        FieldValues = NormalizeRequired(fieldValues, nameof(fieldValues));
        UpdatedAt = updatedAt.ToUniversalTime();
    }

    /// <summary>
    /// Stores the template chosen and the content fetched for it, once, on the save path. Called
    /// again on every save that names a template — re-choosing the same template is idempotent
    /// (fetch-and-overwrite with identical content), and choosing a different one replaces the
    /// snapshot outright. A later edit to that template version at the source never reaches here,
    /// because nothing re-fetches after this call.
    /// </summary>
    public void SnapshotTemplate(
        Guid templateId,
        int templateVersion,
        string htmlContent,
        string? cssContent,
        string? jsContent,
        DateTimeOffset updatedAt)
    {
        if (templateId == Guid.Empty)
        {
            throw new ArgumentException("Template id must not be empty.", nameof(templateId));
        }

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException("Html content is required.", nameof(htmlContent));
        }

        TemplateId = templateId;
        TemplateVersion = templateVersion;
        HtmlContent = htmlContent;
        CssContent = cssContent;
        JsContent = jsContent;
        UpdatedAt = updatedAt.ToUniversalTime();
    }

    /// <summary>
    /// Turns the public event link on. Mints a token only the first time — later calls are a no-op
    /// on the token so an already-shared URL is not silently invalidated by re-enabling.
    /// </summary>
    public void EnablePublicLink(Func<string> tokenGenerator, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrEmpty(PublicEventToken))
        {
            PublicEventToken = tokenGenerator();
        }

        PublicLinkEnabled = true;
        UpdatedAt = updatedAt.ToUniversalTime();
    }

    public void DisablePublicLink(DateTimeOffset updatedAt)
    {
        PublicLinkEnabled = false;
        UpdatedAt = updatedAt.ToUniversalTime();
    }

    /// <summary>
    /// Rotates the public token so the previous URL immediately stops resolving. Leaves the enabled
    /// flag untouched — revoking is about invalidating the old link, not toggling visibility.
    /// </summary>
    public void RevokePublicLink(string newToken, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(newToken))
        {
            throw new ArgumentException("A replacement token is required.", nameof(newToken));
        }

        PublicEventToken = newToken;
        UpdatedAt = updatedAt.ToUniversalTime();
    }

    /// <summary>
    /// Issues (or re-issues) a short-lived preview token. Re-issuing overwrites the previous token,
    /// which is the simplest possible invalidation.
    /// </summary>
    public void IssuePreviewToken(string token, DateTimeOffset expiresAt, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("A preview token is required.", nameof(token));
        }

        PreviewToken = token;
        PreviewExpiresAt = expiresAt.ToUniversalTime();
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
