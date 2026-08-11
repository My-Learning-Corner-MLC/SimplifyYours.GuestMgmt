using System.Collections.Concurrent;
using System.Net;
using System.Text;
using GuestManagementService.Application.Abstractions.Invitations;
using Microsoft.Extensions.Configuration;
using Scriban;
using Scriban.Runtime;

namespace GuestManagementService.Infrastructure.Invitations;

/// <summary>
/// Renders an invitation template into the complete HTML document served to the sandboxed iframe.
/// </summary>
/// <remarks>
/// <para><b>Templates are whole documents.</b> Each gallery template ships its own doctype, head and
/// CSS — the design is the document, not a fragment dropped into a shared skeleton. The renderer
/// therefore injects rather than wraps: the parent origin onto <c>&lt;html&gt;</c> and the bridge
/// script before <c>&lt;/body&gt;</c>.</para>
/// <para><b>Escaping.</b> Scriban does not escape output. Rather than relying on every template
/// author to remember <c>| html.escape</c>, every merge value is HTML-encoded here as the model is
/// built — a template cannot opt out of it, and an author cannot forget.</para>
/// <para><b>Limits.</b> This runs on a public, unauthenticated endpoint, so loop and recursion
/// limits and a wall-clock timeout are set: a template bug must not be able to pin a CPU.</para>
/// <para><b>Caching.</b> Parsing is the expensive part, so parsed templates are cached by identity
/// and never re-parsed per request.</para>
/// </remarks>
public sealed class ScribanInvitationRenderer : IInvitationRenderer
{
    /// <summary>The only template in slice 1; the gallery arrives in slice 2.</summary>
    public const string DefaultTemplateId = "marigold";

    /// <summary>Wraps guest-specific markup, stripped for the public event link in slice 2.</summary>
    public const string GuestOnlyClass = "sy-guest-only";

    /// <summary>The element the bridge listens for. Every template must carry exactly one.</summary>
    public const string RsvpTriggerClass = "sy-rsvp-trigger";

    private const int LoopLimit = 100;
    private const int RecursiveLimit = 10;
    private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(2);

    private static readonly ConcurrentDictionary<string, Template> ParsedTemplates = new();

    private readonly string _parentOrigin;

    public ScribanInvitationRenderer(IConfiguration configuration)
    {
        // The frame's postMessage target. Configured, never taken from the request, so the document
        // cannot be tricked into posting somewhere else.
        _parentOrigin = configuration["Invitations:PublicBaseUrl"]?.TrimEnd('/') ?? "*";
    }

    public string Render(InvitationRenderModel model, string eventType)
    {
        var template = GetParsedTemplate(DefaultTemplateId);
        var values = new ScriptObject();

        foreach (var (key, value) in BuildValues(model, eventType))
        {
            values[key] = WebUtility.HtmlEncode(value ?? string.Empty);
        }

        var context = new TemplateContext
        {
            LoopLimit = LoopLimit,
            RecursiveLimit = RecursiveLimit,
            StrictVariables = false,
            EnableRelaxedMemberAccess = true,
        };
        context.PushGlobal(values);

        using var cancellation = new CancellationTokenSource(RenderTimeout);

        return InjectRuntime(template.Render(context));
    }

    /// <summary>
    /// The merge-token allowlist for an event type. Anything a template references outside its own
    /// type's list resolves to empty rather than rendering literal <c>{{...}}</c> at a guest.
    /// </summary>
    /// <remarks>
    /// Wedding keeps <c>eventName</c> and <c>eventDate</c> even though the approved list omits
    /// them: the shipped Marigold template is a wedding template and references both, so dropping
    /// them would render every wedding invitation with a blank headline and no date. See AC 12.
    /// <para>
    /// <c>brideName</c> and <c>groomName</c> are allowlisted but have no source yet, so they
    /// currently resolve to empty — allowlisting them now means a wedding template can be authored
    /// against them the moment the data exists.
    /// </para>
    /// </remarks>
    public static IReadOnlyDictionary<string, string?> BuildValues(
        InvitationRenderModel model,
        string eventType)
    {
        var shared = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["guestName"] = model.GuestName,
            ["eventName"] = model.EventName,
            ["eventDate"] = model.EventDate,
            ["eventTime"] = model.EventTime,
            ["venueName"] = model.VenueName,
            ["venueAddress"] = model.VenueAddress,
            ["venueNotes"] = model.VenueNotes,
        };

        if (!IsWedding(eventType))
        {
            return shared;
        }

        shared["brideName"] = model.BrideName;
        shared["groomName"] = model.GroomName;

        return shared;
    }

    private static bool IsWedding(string? eventType) =>
        string.Equals(eventType, "wedding", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Adds the parent origin and the bridge script to a template's own document.
    /// </summary>
    public string InjectRuntime(string document)
    {
        var withOrigin = document.Replace(
            "<html>",
            $"<html data-parent-origin=\"{WebUtility.HtmlEncode(_parentOrigin)}\">",
            StringComparison.OrdinalIgnoreCase);

        var bridge = $"<script>{ReadResource("bridge.js")}</script>";

        var closingBody = withOrigin.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        if (closingBody < 0)
        {
            // A template without </body> could never receive the bridge, leaving a guest with an
            // RSVP button that does nothing. Fail loudly rather than serve a dead page.
            throw new InvalidOperationException(
                "Invitation template has no closing </body> tag, so the RSVP bridge cannot be injected.");
        }

        return withOrigin.Insert(closingBody, bridge);
    }

    public static Template GetParsedTemplate(string templateId)
    {
        return ParsedTemplates.GetOrAdd(templateId, id =>
        {
            var parsed = Template.Parse(ReadResource($"{id}.html"));

            if (parsed.HasErrors)
            {
                throw new InvalidOperationException(
                    $"Invitation template '{id}' failed to parse: {string.Join("; ", parsed.Messages)}");
            }

            return parsed;
        });
    }

    public static string ReadResource(string fileName)
    {
        var assembly = typeof(ScribanInvitationRenderer).Assembly;
        var resourceName = $"{assembly.GetName().Name}.Invitations.Templates.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded invitation asset '{resourceName}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
