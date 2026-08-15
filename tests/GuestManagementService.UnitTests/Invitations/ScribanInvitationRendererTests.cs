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

    private static readonly Guid TemplateId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    /// <summary>
    /// A minimal template authored to the §7.3 contract: guest markup AND the RSVP trigger share
    /// one <c>{{ if guestName }}</c> block.
    /// </summary>
    private const string TemplateHtml = """
        <!DOCTYPE html>
        <html>
        <head><style>.placeholder{color:#C98D6B}</style></head>
        <body>
        <div class="invite">
          <h1>{{ brideName }} &amp; {{ groomName }}</h1>
          <p>{{ eventDate }} at {{ eventTime }}</p>
          <p>{{ venueName }}, {{ venueAddress }}</p>
          <p>{{ venueNotes }}</p>
          {{ if guestName }}
          <div class="guest-box">A seat is saved for {{ guestName }}</div>
          <button class="sy-rsvp-trigger">RSVP</button>
          {{ end }}
        </div>
        </body>
        </html>
        """;

    private static InvitationTemplateSnapshot Snapshot(
        string? html = null, string? css = null, string? js = null, int version = 1) =>
        new(TemplateId, version, html ?? TemplateHtml, css, js);

    /// <summary>The content an organiser saved, as the renderer receives it.</summary>
    private static Dictionary<string, string?> Values() => new(StringComparer.Ordinal)
    {
        ["brideName"] = "Amara",
        ["groomName"] = "Julian",
        ["eventName"] = "Amara & Julian",
        ["eventDate"] = "Saturday, September 12, 2026",
        ["eventTime"] = "18:30",
        ["venueName"] = "Villa Astoria",
        ["venueAddress"] = "Lake Como",
        ["venueNotes"] = "Parking at the rear",
    };

    [Fact]
    public async Task Render_FillsTheAllowlistedTokens()
    {
        var html = await Renderer.RenderAsync(Snapshot(), Values(), "Priya Nair", Wedding);

        Assert.Contains("Priya Nair", html, StringComparison.Ordinal);
        Assert.Contains("Villa Astoria", html, StringComparison.Ordinal);
        Assert.Contains("Lake Como", html, StringComparison.Ordinal);
        Assert.Contains("Saturday, September 12, 2026", html, StringComparison.Ordinal);
        Assert.Contains("Amara", html, StringComparison.Ordinal);
        Assert.Contains("Julian", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_EscapesMarkupInMergeValues()
    {
        // The single most important test here: a guest name is attacker-influenced text rendered on
        // a public page. It must appear as literal text, never as markup.
        var html = await Renderer.RenderAsync(Snapshot(), Values(), "Ben & <b>Jerry</b>", Wedding);

        Assert.Contains("Ben &amp; &lt;b&gt;Jerry&lt;/b&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Jerry</b>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_EscapesAScriptTagInAMergeValue()
    {
        var html = await Renderer.RenderAsync(Snapshot(), Values(), "<script>alert(1)</script>", Wedding);

        Assert.DoesNotContain("<script>alert(1)</script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_KeepsTheTemplatesOwnDocumentAndStyling()
    {
        var html = await Renderer.RenderAsync(Snapshot(), Values(), "Ada", Wedding);

        Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, CountOccurrences(html, "<!DOCTYPE"));
        Assert.Equal(1, CountOccurrences(html, "<body"));
        Assert.Contains("#C98D6B", html, StringComparison.Ordinal);
        Assert.Contains("A seat is saved for", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_InjectsCssIntoHeadAndJsBeforeTheBridge()
    {
        // B4: css into <head>, template js before </body>, and the bridge injected LAST so template
        // script can never load after it and shadow its click listener.
        var html = await Renderer.RenderAsync(
            Snapshot(css: "body{color:red}", js: "console.log('template');"),
            Values(),
            "Ada",
            Wedding);

        Assert.Contains("<style>body{color:red}</style>", html, StringComparison.Ordinal);

        var headClose = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        var styleIndex = html.IndexOf("<style>body{color:red}</style>", StringComparison.Ordinal);
        Assert.True(styleIndex < headClose, "css must land inside <head>.");

        var templateScriptIndex = html.IndexOf("console.log('template');", StringComparison.Ordinal);
        var bridgeIndex = html.IndexOf("sy:rsvp", StringComparison.Ordinal);
        Assert.True(templateScriptIndex >= 0, "template js must be present.");
        Assert.True(
            templateScriptIndex < bridgeIndex,
            "the bridge must be injected LAST, after any template js, so it cannot be shadowed.");
    }

    [Fact]
    public async Task Render_WithNoCssOrJs_ProducesAWellFormedDocumentAnyway()
    {
        var html = await Renderer.RenderAsync(Snapshot(), Values(), "Ada", Wedding);

        Assert.Equal(1, CountOccurrences(html, "<style>"));   // only the template's own inline css, if any
        Assert.Contains("sy:rsvp", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_InjectsTheBridgeBeforeTheClosingBodyTag()
    {
        var html = await Renderer.RenderAsync(Snapshot(), Values(), "Ada", Wedding);

        Assert.Contains("sy:rsvp", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("sy:rsvp", StringComparison.Ordinal) < html.LastIndexOf("</body>", StringComparison.Ordinal),
            "The bridge script must sit inside the document body.");
    }

    [Fact]
    public async Task Render_PinsThePostMessageTargetToTheConfiguredOrigin()
    {
        var html = await Renderer.RenderAsync(Snapshot(), Values(), "Ada", Wedding);

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
    public void AssembleDocument_ThrowsWhenJsIsPresentButThereIsNoClosingBody()
    {
        Assert.Throws<InvalidOperationException>(
            () => ScribanInvitationRenderer.AssembleDocument("<html><head></head></html>", null, "alert(1)"));
    }

    // ---------- B5: guest-absent rendering (Scriban truthiness trap) ----------

    [Fact]
    public async Task Render_WithARealGuestName_RendersTheGuestBlockAndTheRsvpTrigger()
    {
        var html = await Renderer.RenderAsync(Snapshot(), Values(), "Priya Nair", Wedding);

        Assert.Contains("guest-box", html, StringComparison.Ordinal);
        Assert.Contains("sy-rsvp-trigger", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_WithGuestNameAbsentFromTheModel_DropsBothTheGuestBlockAndTheRsvpTrigger()
    {
        var html = await Renderer.RenderAsync(Snapshot(), Values(), guestName: null, Wedding);

        Assert.DoesNotContain("guest-box", html, StringComparison.Ordinal);
        // Checked as the button's class attribute, not a bare substring: the always-injected bridge
        // script also references '.sy-rsvp-trigger' as a selector, so a bare Contains would always
        // pass regardless of whether the template's own button rendered.
        Assert.DoesNotContain("class=\"sy-rsvp-trigger\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_WithGuestNameBoundToEmptyString_StillDropsBothBlocks()
    {
        // THE TRAP TEST. Scriban's `{{ if guestName }}` treats only null/false as falsy — an empty
        // string is truthy. If this assertion ever fails, guestName is being bound as "" somewhere
        // instead of omitted, and the public event link leaks an empty guest box and a live-looking
        // RSVP trigger onto an anonymous page.
        var html = await Renderer.RenderAsync(Snapshot(), Values(), guestName: "", Wedding);

        Assert.DoesNotContain("guest-box", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"sy-rsvp-trigger\"", html, StringComparison.Ordinal);
    }

    // ---------- B3: the parse cache must key on (templateId, templateVersion) ----------

    [Fact]
    public void GetParsedTemplate_CachesByTemplateIdAndVersionTogether()
    {
        var first = ScribanInvitationRenderer.GetParsedTemplate(Snapshot(version: 1));
        var second = ScribanInvitationRenderer.GetParsedTemplate(Snapshot(version: 1));
        var differentVersion = ScribanInvitationRenderer.GetParsedTemplate(Snapshot(version: 2));

        Assert.Same(first, second);
        Assert.NotSame(first, differentVersion);
    }

    [Fact]
    public async Task Render_TwoVersionsOfTheSameTemplateId_RenderTheirOwnSnapshotContentNotEachOthers()
    {
        // The subtle break §3.2 warns about: a cache keyed on templateId ALONE would serve one
        // event's snapshot to another event that references the same template id under a different
        // version. Two distinct html bodies, same TemplateId, different TemplateVersion.
        //
        // A fresh, test-local template id keeps this independent of the process-wide static parse
        // cache that every other test in this class also populates for the shared TemplateId.
        var freshTemplateId = Guid.NewGuid();
        const string v1Html = "<html><body>Version one content {{ eventDate }}</body></html>";
        const string v2Html = "<html><body>Version two content {{ eventDate }}</body></html>";

        var renderedV1 = await Renderer.RenderAsync(
            new InvitationTemplateSnapshot(freshTemplateId, 1, v1Html, null, null), Values(), "Ada", Wedding);
        var renderedV2 = await Renderer.RenderAsync(
            new InvitationTemplateSnapshot(freshTemplateId, 2, v2Html, null, null), Values(), "Ada", Wedding);

        Assert.Contains("Version one content", renderedV1, StringComparison.Ordinal);
        Assert.DoesNotContain("Version two content", renderedV1, StringComparison.Ordinal);

        Assert.Contains("Version two content", renderedV2, StringComparison.Ordinal);
        Assert.DoesNotContain("Version one content", renderedV2, StringComparison.Ordinal);
    }

    [Fact]
    public void GetParsedTemplate_ThrowsForATemplateThatFailsToParse()
    {
        var broken = Snapshot(html: "<html><body>{{ if unterminated</body></html>", version: 99);

        Assert.Throws<InvalidOperationException>(() => ScribanInvitationRenderer.GetParsedTemplate(broken));
    }

    [Fact]
    public void BuildValues_ForBirthday_ExposesTheBirthdayAllowlist()
    {
        var keys = ScribanInvitationRenderer.BuildValues(Values(), "Ada", Birthday)
            .Keys.OrderBy(k => k, StringComparer.Ordinal);

        Assert.Equal(
            new[] { "eventDate", "eventName", "eventTime", "guestName", "venueAddress", "venueName", "venueNotes" },
            keys);
    }

    [Fact]
    public void BuildValues_ForWedding_AddsTheCoupleNames()
    {
        var keys = ScribanInvitationRenderer.BuildValues(Values(), "Ada", Wedding)
            .Keys.OrderBy(k => k, StringComparer.Ordinal);

        Assert.Equal(
            new[]
            {
                "brideName", "eventDate", "eventTime", "groomName",
                "guestName", "venueAddress", "venueName", "venueNotes",
            },
            keys);
    }

    [Fact]
    public void BuildValues_DoesNotLeakCoupleNamesIntoBirthdayTemplates()
    {
        var keys = ScribanInvitationRenderer.BuildValues(Values(), "Ada", Birthday).Keys;

        Assert.DoesNotContain("brideName", keys);
        Assert.DoesNotContain("groomName", keys);
    }

    [Fact]
    public void BuildValues_WithGuestNameNull_BindsTheKeyToNullRatherThanOmittingIt()
    {
        // Either shape satisfies Scriban truthiness (§7.3), but the renderer's contract is to keep
        // the key present and null — this pins that choice so a future refactor cannot silently
        // switch to binding "" instead.
        var values = ScribanInvitationRenderer.BuildValues(Values(), null, Wedding);

        Assert.True(values.ContainsKey("guestName"));
        Assert.Null(values["guestName"]);
    }

    private static int CountOccurrences(string source, string value) =>
        source.Split(value).Length - 1;
}
