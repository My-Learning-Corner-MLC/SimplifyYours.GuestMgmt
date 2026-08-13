using System.Collections.Concurrent;
using System.Net;
using System.Text;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Invitations;
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

    /// <summary>
    /// The bridge never varies, so it is read from the assembly manifest once. The parsed template
    /// beside it was already cached; this was being re-read on every single render.
    /// </summary>
    private static readonly string BridgeScript = ReadResource("bridge.js");

    private readonly string _parentOrigin;

    public ScribanInvitationRenderer(IConfiguration configuration)
    {
        // The frame's postMessage target. Configured, never taken from the request, so the document
        // cannot be tricked into posting somewhere else.
        //
        // No fallback. A missing key used to degrade to "*", which posts to any origin and hides the
        // misconfiguration behind a page that still looks healthy. InvitationLinkBuilder already
        // throws on this same key, so failing at startup is both safer and consistent.
        var configured = configuration["Invitations:PublicBaseUrl"];

        // Blank counts as missing: appsettings.json ships the key empty precisely so a deployment
        // that forgets to override it fails here instead of serving invitations that look fine and
        // do nothing.
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                "Invitations:PublicBaseUrl is not configured. It is the origin the invitation frame "
                + "posts its RSVP message to, and the origin named in the frame-ancestors CSP; "
                + "without it every invitation renders unusable.");
        }

        _parentOrigin = configured.TrimEnd('/');
    }

    public async Task<string> RenderAsync(
        IReadOnlyDictionary<string, string?> fieldValues,
        string guestName,
        string eventType)
    {
        var template = GetParsedTemplate(DefaultTemplateId);
        var values = new ScriptObject();

        // The organiser is now an input source for a public page, so their text is escaped exactly
        // as a guest's name always has been. Neither is trusted over the other.
        foreach (var (key, value) in BuildValues(fieldValues, guestName, eventType))
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

        // Scriban reads this during async evaluation only. Assigning it and then calling the
        // synchronous Render() — as this did — allocates a timer that nothing ever observes, so the
        // documented wall-clock bound silently did not exist.
        context.CancellationToken = cancellation.Token;

        return InjectRuntime(await template.RenderAsync(context));
    }

    /// <summary>
    /// The merge-token allowlist for an event type. Anything a template references outside its own
    /// type's list resolves to empty rather than rendering literal <c>{{...}}</c> at a guest.
    /// </summary>
    /// <remarks>
    /// The per-type field sets live in <see cref="InvitationFieldSchema"/> and are the single
    /// source of truth for AC 12. <c>guestName</c> is added here because it is the one value an
    /// organiser never types — it belongs to whichever guest opened the link.
    /// </remarks>
    public static IReadOnlyDictionary<string, string?> BuildValues(
        IReadOnlyDictionary<string, string?> fieldValues,
        string guestName,
        string eventType)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            // Never typed by the organiser — it is whichever guest opened the link.
            ["guestName"] = guestName,
        };

        // Only fields the event type declares. A value saved under a key this type does not use
        // cannot reach the template, so the allowlist holds even if storage somehow contains more.
        foreach (var field in InvitationFieldSchema.AllFor(eventType))
        {
            values[field] = fieldValues.TryGetValue(field, out var value) ? value : null;
        }

        return values;
    }

    /// <summary>
    /// Adds the parent origin and the bridge script to a template's own document.
    /// </summary>
    public string InjectRuntime(string document)
    {
        var withOrigin = document.Replace(
            "<html>",
            $"<html data-parent-origin=\"{WebUtility.HtmlEncode(_parentOrigin)}\">",
            StringComparison.OrdinalIgnoreCase);

        var bridge = $"<script>{BridgeScript}</script>";

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
