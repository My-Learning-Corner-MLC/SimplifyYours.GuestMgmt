using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using GuestManagementService.Application.Abstractions.Invitations;
using Microsoft.Extensions.Configuration;
using Scriban;
using Scriban.Runtime;

namespace GuestManagementService.Infrastructure.Invitations;

/// <summary>
/// Renders the invitation document with Scriban.
/// </summary>
/// <remarks>
/// <para><b>Escaping.</b> Scriban does not escape output. Rather than relying on every template
/// author to remember <c>| html.escape</c>, every merge value is HTML-encoded here as the model is
/// built — a template cannot opt out of it, and a template author cannot forget.</para>
/// <para><b>Limits.</b> This runs on a public, unauthenticated endpoint, so loop and recursion
/// limits and a wall-clock timeout are set: a template bug must not be able to pin a CPU.</para>
/// <para><b>Caching.</b> Parsing is the expensive part, so parsed templates are cached by identity
/// and never re-parsed per request.</para>
/// </remarks>
public sealed class ScribanInvitationRenderer : IInvitationRenderer
{
    public const string DefaultTemplateId = "default-invitation";

    private const int LoopLimit = 100;
    private const int RecursiveLimit = 10;
    private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(2);

    private static readonly ConcurrentDictionary<string, Template> ParsedTemplates = new();

    private readonly string _parentOrigin;

    public ScribanInvitationRenderer(IConfiguration configuration)
    {
        // The frame's postMessage target. Configured, never taken from the request, so the document
        // cannot be tricked into posting to somewhere else.
        _parentOrigin = configuration["Invitations:PublicBaseUrl"]?.TrimEnd('/') ?? "*";
    }

    public string Render(InvitationRenderModel model)
    {
        var template = GetParsedTemplate(DefaultTemplateId);

        var values = new ScriptObject();

        // Exactly the allowlisted tokens, each HTML-encoded. Anything else a template references
        // resolves to empty rather than rendering literal "{{...}}" at a guest.
        foreach (var (key, value) in BuildValues(model))
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
        var body = template.Render(context);

        return BuildDocument(model.EventName, body);
    }

    public static IReadOnlyDictionary<string, string?> BuildValues(InvitationRenderModel model)
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["guestName"] = model.GuestName,
            ["eventName"] = model.EventName,
            ["eventDate"] = model.EventDate,
            ["eventTime"] = model.EventTime,
            ["venueName"] = model.VenueName,
            ["venueAddress"] = model.VenueAddress,
            ["eventDescription"] = model.EventDescription,
        };
    }

    public static Template GetParsedTemplate(string templateId)
    {
        return ParsedTemplates.GetOrAdd(templateId, id =>
        {
            var parsed = Template.Parse(ReadResource($"{id}.html"));

            if (parsed.HasErrors)
            {
                // Fail at startup/first use with the actual parse errors rather than emitting a
                // broken page to a guest.
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

    private string BuildDocument(string eventName, string body)
    {
        var css = ReadResource($"{DefaultTemplateId}.css");
        var bridge = ReadResource("bridge.js");

        return $"""
            <!doctype html>
            <html lang="en" data-parent-origin="{WebUtility.HtmlEncode(_parentOrigin)}">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="robots" content="noindex, nofollow">
            <title>{WebUtility.HtmlEncode(eventName)}</title>
            <style>{css}</style>
            </head>
            <body>
            {body}
            <script>{bridge}</script>
            </body>
            </html>
            """;
    }
}
