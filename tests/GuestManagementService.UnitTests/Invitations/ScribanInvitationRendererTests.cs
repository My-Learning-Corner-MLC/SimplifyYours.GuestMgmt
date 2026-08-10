using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Infrastructure.Invitations;
using Microsoft.Extensions.Configuration;

namespace GuestManagementService.UnitTests.Invitations;

public sealed class ScribanInvitationRendererTests
{
    private static readonly ScribanInvitationRenderer Renderer = new(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Invitations:PublicBaseUrl"] = "https://app.example.test",
            })
            .Build());

    private static InvitationRenderModel Model(string guestName = "Ada") =>
        new(guestName, "Eleanor & Sam", "12 September 2026", "18:30", "Rosewood Hall", "12 Sample Street", "An evening reception");

    [Fact]
    public void Render_FillsTheAllowlistedTokens()
    {
        var html = Renderer.Render(Model());

        Assert.Contains("Dear Ada,", html, StringComparison.Ordinal);
        Assert.Contains("Rosewood Hall", html, StringComparison.Ordinal);
        Assert.Contains("12 September 2026", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_EscapesMarkupInMergeValues()
    {
        // The single most important test here: a guest name is attacker-influenced text rendered on
        // a public page. It must appear as literal text, never as markup.
        var html = Renderer.Render(Model("Ben & <b>Jerry</b>"));

        Assert.Contains("Ben &amp; &lt;b&gt;Jerry&lt;/b&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Jerry</b>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_EscapesAScriptTagInAMergeValue()
    {
        var html = Renderer.Render(Model("<script>alert(1)</script>"));

        Assert.DoesNotContain("<script>alert(1)</script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_LeavesNoUnresolvedPlaceholdersVisibleToAGuest()
    {
        var html = Renderer.Render(Model());

        // An unknown token must render empty, never as literal "{{ ... }}" text on the page.
        Assert.DoesNotContain("{{", html, StringComparison.Ordinal);
        Assert.DoesNotContain("}}", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ProducesACompleteDocumentWithInlinedCssAndTheBridge()
    {
        var html = Renderer.Render(Model());

        Assert.StartsWith("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<style>", html, StringComparison.Ordinal);
        Assert.Contains(".sy-rsvp-trigger", html, StringComparison.Ordinal);
        Assert.Contains("sy:rsvp", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_PinsThePostMessageTargetToTheConfiguredOrigin()
    {
        // The frame must not be able to post to an arbitrary origin.
        var html = Renderer.Render(Model());

        Assert.Contains("data-parent-origin=\"https://app.example.test\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_ContainsExactlyOneRsvpTrigger()
    {
        // Guards the authoring contract: a template shipped without a trigger would leave guests
        // physically unable to respond, and two would double-fire the dialog.
        var source = ScribanInvitationRenderer.ReadResource($"{ScribanInvitationRenderer.DefaultTemplateId}.html");

        var occurrences = source.Split("sy-rsvp-trigger").Length - 1;

        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void GetParsedTemplate_CachesTheParsedTemplate()
    {
        // Parsing is the expensive part; re-parsing on every public request would be a free DoS.
        var first = ScribanInvitationRenderer.GetParsedTemplate(ScribanInvitationRenderer.DefaultTemplateId);
        var second = ScribanInvitationRenderer.GetParsedTemplate(ScribanInvitationRenderer.DefaultTemplateId);

        Assert.Same(first, second);
    }

    [Fact]
    public void BuildValues_ExposesExactlyTheAllowlistedTokens()
    {
        var keys = ScribanInvitationRenderer.BuildValues(Model()).Keys.OrderBy(k => k, StringComparer.Ordinal);

        Assert.Equal(
            new[] { "eventDate", "eventDescription", "eventName", "eventTime", "guestName", "venueAddress", "venueName" },
            keys);
    }
}
