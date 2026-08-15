namespace GuestManagementService.Application.Abstractions.Invitations;

/// <summary>
/// The current version of one catalog template, as returned by
/// <c>GET /templates/{id}</c> on <c>template-management-service</c>.
/// </summary>
public sealed record TemplateCatalogEntry(
    Guid Id,
    string Name,
    string EventType,
    int Version,
    string HtmlContent,
    string? CssContent,
    string? JsContent);

public enum TemplateFetchStatus
{
    Found = 0,
    NotFound = 1,

    /// <summary>The service could not be reached, or returned something other than 200/404.</summary>
    Unavailable = 2,
}

public sealed record TemplateFetchResult(TemplateFetchStatus Status, TemplateCatalogEntry? Template = null)
{
    public static TemplateFetchResult Found(TemplateCatalogEntry template) =>
        new(TemplateFetchStatus.Found, template);

    public static TemplateFetchResult NotFound() => new(TemplateFetchStatus.NotFound);

    public static TemplateFetchResult Unavailable() => new(TemplateFetchStatus.Unavailable);
}

/// <summary>
/// Fetches one template's current version from <c>template-management-service</c>.
/// </summary>
/// <remarks>
/// <b>Called from exactly one place: the authenticated invitation-settings save path.</b> The
/// anonymous render path only ever reads the local snapshot on <c>EventInvitationSettings</c>, so a
/// <c>template-management-service</c> outage can never take invitations down — see
/// <c>SaveInvitationSettingsCommandHandler</c>. Never inject this into anything reachable from
/// <c>InvitationEndpoints</c>.
/// </remarks>
public interface ITemplateCatalogClient
{
    Task<TemplateFetchResult> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken);
}
