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
/// <para><b>Templates are whole documents, assembled from a snapshot.</b> <c>HtmlContent</c> ships
/// its own doctype, head and body. The renderer injects rather than wraps: <c>CssContent</c> into
/// <c>&lt;head&gt;</c>, <c>JsContent</c> before <c>&lt;/body&gt;</c>, then the parent origin onto
/// <c>&lt;html&gt;</c> and the bridge script — in that order, bridge last, so template script can
/// never load after the bridge and shadow its event listener.</para>
/// <para><b>Escaping.</b> Scriban does not escape output. Rather than relying on every template
/// author to remember <c>| html.escape</c>, every merge value is HTML-encoded here as the model is
/// built — a template cannot opt out of it, and an author cannot forget.</para>
/// <para><b>Limits.</b> This runs on a public, unauthenticated endpoint, so loop and recursion
/// limits and a wall-clock timeout are set: a template bug must not be able to pin a CPU.</para>
/// <para><b>Caching.</b> Parsing is the expensive part, so parsed templates are cached by the pair
/// <c>(templateId, templateVersion)</c> — never by id alone. Content now arrives per snapshot, so
/// two events can reference the same template id while holding different versions' content; a
/// cache keyed on id alone would serve one event's snapshot to the other.</para>
/// </remarks>
public sealed class ScribanInvitationRenderer : IInvitationRenderer
{
    /// <summary>The element the bridge listens for. Every template must carry exactly one.</summary>
    public const string RsvpTriggerClass = "sy-rsvp-trigger";

    private const int LoopLimit = 100;
    private const int RecursiveLimit = 10;
    private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(2);

    private static readonly ConcurrentDictionary<(Guid TemplateId, int TemplateVersion), Template> ParsedTemplates = new();

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
        InvitationTemplateSnapshot snapshot,
        IReadOnlyDictionary<string, string?> fieldValues,
        string? guestName,
        string eventType)
    {
        var template = GetParsedTemplate(snapshot);
        var values = new ScriptObject();

        // Defence in depth: an empty string is truthy to Scriban, so if some caller ever passes ""
        // instead of omitting guestName, this still collapses it to null before it reaches the
        // model. The resolver (ResolveInvitationRenderQueryHandler) is the primary guard — this is
        // the backstop the trap test in ScribanInvitationRendererTests pins.
        var normalizedGuestName = string.IsNullOrEmpty(guestName) ? null : guestName;

        // The organiser is now an input source for a public page, so their text is escaped exactly
        // as a guest's name always has been. Neither is trusted over the other.
        //
        // A null value is bound as Scriban null, never as an encoded empty string — Scriban's
        // `{{ if guestName }}` treats only null/false as falsy, and an empty string is truthy. For
        // guestName specifically this is what keeps the public event link and public preview mode
        // from leaking an empty guest box and a live-looking RSVP trigger. See BuildValues.
        foreach (var (key, value) in BuildValues(fieldValues, normalizedGuestName, eventType))
        {
            values[key] = value is null ? null : WebUtility.HtmlEncode(value);
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

        var rendered = await template.RenderAsync(context);
        var assembled = AssembleDocument(rendered, snapshot.CssContent, snapshot.JsContent);

        return InjectRuntime(assembled);
    }

    private const string InvitationFieldSchema_GuestNameKey = "guestName";

    /// <summary>
    /// The merge-token allowlist for an event type. Anything a template references outside its own
    /// type's list resolves to empty rather than rendering literal <c>{{...}}</c> at a guest.
    /// </summary>
    /// <remarks>
    /// The per-type field sets live in <see cref="InvitationFieldSchema"/> and are the single
    /// source of truth. <c>guestName</c> is added here because it is the one value an organiser
    /// never types — it belongs to whichever guest opened the link, and is <see langword="null"/>
    /// (key present, value null) when no guest is bound, so the caller can distinguish "omit the
    /// key" from "field not collected for this event type".
    /// </remarks>
    public static IReadOnlyDictionary<string, string?> BuildValues(
        IReadOnlyDictionary<string, string?> fieldValues,
        string? guestName,
        string eventType)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            // Never typed by the organiser — it is whichever guest opened the link, or null when no
            // guest is bound (public event link, public preview).
            [InvitationFieldSchema_GuestNameKey] = guestName,
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
    /// Composes the served document from the snapshot's three parts. <paramref name="html"/> already
    /// carries its own doctype and <c>&lt;head&gt;</c>; css goes into a <c>&lt;style&gt;</c> block
    /// inside it, js goes into a <c>&lt;script&gt;</c> immediately before <c>&lt;/body&gt;</c>.
    /// </summary>
    public static string AssembleDocument(string html, string? css, string? js)
    {
        var document = html;

        if (!string.IsNullOrWhiteSpace(css))
        {
            var headClose = document.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);

            if (headClose >= 0)
            {
                document = document.Insert(headClose, $"<style>{css}</style>");
            }
        }

        if (!string.IsNullOrWhiteSpace(js))
        {
            var bodyClose = document.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

            if (bodyClose < 0)
            {
                throw new InvalidOperationException(
                    "Invitation template has no closing </body> tag, so template script cannot be injected.");
            }

            document = document.Insert(bodyClose, $"<script>{js}</script>");
        }

        return document;
    }

    /// <summary>
    /// Adds the parent origin and the bridge script to the assembled document. The bridge is
    /// injected LAST — after any template <c>js_content</c> — so template script can never load
    /// after it and shadow its <c>click</c> listener.
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

    public static Template GetParsedTemplate(InvitationTemplateSnapshot snapshot)
    {
        var key = (snapshot.TemplateId, snapshot.TemplateVersion);

        return ParsedTemplates.GetOrAdd(key, _ =>
        {
            var parsed = Template.Parse(snapshot.HtmlContent);

            if (parsed.HasErrors)
            {
                throw new InvalidOperationException(
                    $"Invitation template '{snapshot.TemplateId}' v{snapshot.TemplateVersion} failed "
                    + $"to parse: {string.Join("; ", parsed.Messages)}");
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
