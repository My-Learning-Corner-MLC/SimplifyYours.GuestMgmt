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

    private const string Wedding = "wedding";
    private const string Birthday = "birthday";

    private static InvitationRenderModel Model(string guestName = "Ada") =>
        new(
            guestName,
            "Amara & Julian",
            "Saturday, September 12, 2026",
            "18:30",
            "Villa Astoria",
            "Lake Como",
            "Parking at the rear",
            BrideName: "Amara",
            GroomName: "Julian");

    [Fact]
    public void Render_FillsTheAllowlistedTokens()
    {
        var html = Renderer.Render(Model("Priya Nair"), Wedding);

        Assert.Contains("Priya Nair", html, StringComparison.Ordinal);
        Assert.Contains("Villa Astoria", html, StringComparison.Ordinal);
        Assert.Contains("Lake Como", html, StringComparison.Ordinal);
        Assert.Contains("Saturday, September 12, 2026", html, StringComparison.Ordinal);
        // "&" in the event name must survive as an entity, not as raw markup.
        Assert.Contains("Amara &amp; Julian", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_EscapesMarkupInMergeValues()
    {
        // The single most important test here: a guest name is attacker-influenced text rendered on
        // a public page. It must appear as literal text, never as markup.
        var html = Renderer.Render(Model("Ben & <b>Jerry</b>"), Wedding);

        Assert.Contains("Ben &amp; &lt;b&gt;Jerry&lt;/b&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Jerry</b>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_EscapesAScriptTagInAMergeValue()
    {
        var html = Renderer.Render(Model("<script>alert(1)</script>"), Wedding);

        Assert.DoesNotContain("<script>alert(1)</script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_LeavesNoUnresolvedPlaceholdersVisibleToAGuest()
    {
        var html = Renderer.Render(Model(), Wedding);

        Assert.DoesNotContain("{{", html, StringComparison.Ordinal);
        Assert.DoesNotContain("}}", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_KeepsTheTemplatesOwnDocumentAndStyling()
    {
        // Marigold ships its own doctype, head and CSS — the design is the document. The renderer
        // must inject into it, never wrap it in a second skeleton.
        var html = Renderer.Render(Model(), Wedding);

        Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, CountOccurrences(html, "<!DOCTYPE"));
        Assert.Equal(1, CountOccurrences(html, "<body"));
        Assert.Contains("#C98D6B", html, StringComparison.Ordinal);   // Marigold terracotta field
        Assert.Contains("A seat is saved for", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_InjectsTheBridgeBeforeTheClosingBodyTag()
    {
        var html = Renderer.Render(Model(), Wedding);

        Assert.Contains("sy:rsvp", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("sy:rsvp", StringComparison.Ordinal) < html.LastIndexOf("</body>", StringComparison.Ordinal),
            "The bridge script must sit inside the document body.");
    }

    [Fact]
    public void Render_PinsThePostMessageTargetToTheConfiguredOrigin()
    {
        // The frame must not be able to post to an arbitrary origin.
        var html = Renderer.Render(Model(), Wedding);

        Assert.Contains("data-parent-origin=\"https://app.example.test\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void InjectRuntime_ThrowsWhenTheTemplateHasNoClosingBody()
    {
        // Such a template would ship an RSVP button that does nothing. Better to fail loudly than
        // to serve a guest a dead page.
        Assert.Throws<InvalidOperationException>(
            () => Renderer.InjectRuntime("<html><head></head></html>"));
    }

    [Fact]
    public void Template_ContainsExactlyOneRsvpTriggerAndOneGuestOnlyBlock()
    {
        // Guards the authoring contract. No trigger leaves guests unable to respond at all; two
        // would double-fire the dialog. The guest-only block is what slice 2 strips for the public
        // event link, so it has to be present and singular now.
        var source = ScribanInvitationRenderer.ReadResource($"{ScribanInvitationRenderer.DefaultTemplateId}.html");

        // Counts the class *attribute*, not every mention — a template also names these classes in
        // its CSS, and styling them any number of times is fine.
        Assert.Equal(1, CountOccurrences(source, $"class=\"{ScribanInvitationRenderer.RsvpTriggerClass}\""));
        Assert.Equal(1, CountOccurrences(source, $"class=\"{ScribanInvitationRenderer.GuestOnlyClass}\""));
    }

    [Fact]
    public void Template_ReferencesOnlyAllowlistedTokens()
    {
        var source = ScribanInvitationRenderer.ReadResource($"{ScribanInvitationRenderer.DefaultTemplateId}.html");
        var allowed = ScribanInvitationRenderer.BuildValues(Model(), Wedding).Keys.ToHashSet(StringComparer.Ordinal);

        foreach (var token in ExtractTokens(source))
        {
            Assert.True(allowed.Contains(token), $"Template references non-allowlisted token '{token}'.");
        }
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
    public void BuildValues_ForBirthday_ExposesTheBirthdayAllowlist()
    {
        var keys = ScribanInvitationRenderer.BuildValues(Model(), Birthday)
            .Keys.OrderBy(k => k, StringComparer.Ordinal);

        Assert.Equal(
            new[] { "eventDate", "eventName", "eventTime", "guestName", "venueAddress", "venueName", "venueNotes" },
            keys);
    }

    [Fact]
    public void BuildValues_ForWedding_AddsTheCoupleNames()
    {
        var keys = ScribanInvitationRenderer.BuildValues(Model(), Wedding)
            .Keys.OrderBy(k => k, StringComparer.Ordinal);

        // eventName and eventDate are retained for wedding despite being absent from the approved
        // list: Marigold is a wedding template and references both. See AC 12.
        Assert.Equal(
            new[]
            {
                "brideName", "eventDate", "eventName", "eventTime", "groomName",
                "guestName", "venueAddress", "venueName", "venueNotes",
            },
            keys);
    }

    [Fact]
    public void BuildValues_DoesNotLeakCoupleNamesIntoBirthdayTemplates()
    {
        var keys = ScribanInvitationRenderer.BuildValues(Model(), Birthday).Keys;

        Assert.DoesNotContain("brideName", keys);
        Assert.DoesNotContain("groomName", keys);
    }

    private static int CountOccurrences(string source, string value) =>
        source.Split(value).Length - 1;

    private static IEnumerable<string> ExtractTokens(string source)
    {
        var index = 0;

        while ((index = source.IndexOf("{{", index, StringComparison.Ordinal)) >= 0)
        {
            var end = source.IndexOf("}}", index, StringComparison.Ordinal);

            if (end < 0)
            {
                yield break;
            }

            yield return source[(index + 2)..end].Trim();
            index = end + 2;
        }
    }
}
