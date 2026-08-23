using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GuestManagementService.Application.Abstractions.Invitations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.Infrastructure.Invitations;

/// <summary>
/// HTTP client for <c>template-management-service</c>'s catalog.
/// </summary>
/// <remarks>
/// Registered as a typed <see cref="HttpClient"/> (<c>services.AddHttpClient&lt;ITemplateCatalogClient,
/// TemplateCatalogClient&gt;</c>) with its base address bound to <c>TemplateManagement:BaseUrl</c>,
/// mirroring how <c>Invitations:PublicBaseUrl</c> is read directly off <see cref="IConfiguration"/>
/// elsewhere in this service rather than through an options type.
/// <para>
/// <b>Forwards the organiser's own bearer token.</b> The catalog requires <c>events.view</c>, and
/// this call happens only on the authenticated save path (never on the anonymous render path — see
/// <c>ITemplateCatalogClient</c>'s remarks), so the caller already holds that exact claim. This is a
/// delegated action on the organiser's behalf, not a service identity, which is why it forwards the
/// incoming token rather than minting a separate machine credential. If the incoming request
/// somehow carries no bearer token (should not happen behind <c>RequireAuthorization</c>, but
/// defence in depth), the call proceeds without one and template-management-service correctly
/// rejects it with 401 — surfacing to the organiser as the existing "template catalog could not be
/// reached" failure rather than a confusing crash.
/// </para>
/// <para>
/// Every failure mode collapses to <see cref="TemplateFetchStatus.Unavailable"/> or
/// <see cref="TemplateFetchStatus.NotFound"/> — never an exception — because the only caller,
/// <c>SaveInvitationSettingsCommandHandler</c>, needs to turn any of them into the same synchronous
/// 400 without a try/catch per failure mode.
/// </para>
/// </remarks>
public sealed class TemplateCatalogClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    ILogger<TemplateCatalogClient> logger)
    : ITemplateCatalogClient
{
    public async Task<TemplateFetchResult> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"templates/{templateId:D}");
            AttachBearerToken(request);

            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                exception,
                "template-management-service was unreachable while fetching template {TemplateId}.",
                templateId);

            return TemplateFetchResult.Unavailable();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return TemplateFetchResult.NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "template-management-service returned {StatusCode} for template {TemplateId}.",
                (int)response.StatusCode,
                templateId);

            return TemplateFetchResult.Unavailable();
        }

        TemplateCatalogResponseBody? body;

        try
        {
            body = await response.Content.ReadFromJsonAsync<TemplateCatalogResponseBody>(cancellationToken);
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException)
        {
            logger.LogWarning(
                exception,
                "template-management-service returned an unreadable body for template {TemplateId}.",
                templateId);

            return TemplateFetchResult.Unavailable();
        }

        if (body is null || string.IsNullOrWhiteSpace(body.HtmlContent))
        {
            return TemplateFetchResult.Unavailable();
        }

        return TemplateFetchResult.Found(new TemplateCatalogEntry(
            body.Id,
            body.Name ?? string.Empty,
            body.EventType ?? string.Empty,
            body.Version,
            body.HtmlContent,
            body.CssContent,
            body.JsContent));
    }

    /// <summary>
    /// Copies the incoming request's <c>Authorization</c> header onto the outbound one, verbatim.
    /// Reads the raw header rather than re-deriving a token from <c>CurrentUser</c>, because that
    /// type carries only the parsed claims (user id, tenant id) — not the bearer token itself.
    /// </summary>
    private void AttachBearerToken(HttpRequestMessage request)
    {
        var incoming = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(incoming) || !AuthenticationHeaderValue.TryParse(incoming, out var header))
        {
            return;
        }

        request.Headers.Authorization = header;
    }

    /// <summary>Matches the fixed contract: <c>{ id, name, eventType, version, htmlContent, cssContent, jsContent }</c>.</summary>
    private sealed record TemplateCatalogResponseBody(
        Guid Id,
        string? Name,
        string? EventType,
        int Version,
        string HtmlContent,
        string? CssContent,
        string? JsContent);
}
