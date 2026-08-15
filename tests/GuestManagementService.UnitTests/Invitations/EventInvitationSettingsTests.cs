using GuestManagementService.Domain.Invitations;

namespace GuestManagementService.UnitTests.Invitations;

public sealed class EventInvitationSettingsTests
{
    private static readonly Guid EventId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");
    private static readonly Guid TenantId = Guid.Parse("0fa219ed-70ad-4e8d-9f51-6e60409dc659");
    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StoresTheSnapshotAndFieldValues()
    {
        var settings = Create();

        Assert.Equal(EventId, settings.EventId);
        Assert.Equal(TenantId, settings.TenantId);
        Assert.Equal(TemplateId, settings.TemplateId);
        Assert.Equal(1, settings.TemplateVersion);
        Assert.Equal("<html></html>", settings.HtmlContent);
        Assert.Equal("{\"brideName\":\"Amara\"}", settings.FieldValues);
        Assert.Equal(Now, settings.CreatedAt);
        Assert.Equal(Now, settings.UpdatedAt);
    }

    [Fact]
    public void Create_DefaultsPublicLinkToDisabledWithNoTokens()
    {
        var settings = Create();

        Assert.False(settings.PublicLinkEnabled);
        Assert.Null(settings.PublicEventToken);
        Assert.Null(settings.PreviewToken);
        Assert.Null(settings.PreviewExpiresAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankFieldValues(string fieldValues)
    {
        Assert.Throws<ArgumentException>(() => Create(fieldValues: fieldValues));
    }

    [Fact]
    public void Create_RejectsAnEmptyEventOrTenantId()
    {
        Assert.Throws<ArgumentException>(() =>
            EventInvitationSettings.Create(Guid.Empty, TenantId, "{}", TemplateId, 1, "<html></html>", null, null, Now));
        Assert.Throws<ArgumentException>(() =>
            EventInvitationSettings.Create(EventId, Guid.Empty, "{}", TemplateId, 1, "<html></html>", null, null, Now));
    }

    [Fact]
    public void Create_RejectsAnEmptyTemplateId()
    {
        Assert.Throws<ArgumentException>(() =>
            EventInvitationSettings.Create(EventId, TenantId, "{}", Guid.Empty, 1, "<html></html>", null, null, Now));
    }

    [Fact]
    public void UpdateFieldValues_ReplacesContentAndMovesUpdatedAtOnly()
    {
        var settings = Create();
        var later = Now.AddDays(2);

        settings.UpdateFieldValues("{\"brideName\":\"Priya\"}", later);

        Assert.Equal("{\"brideName\":\"Priya\"}", settings.FieldValues);
        Assert.Equal(later, settings.UpdatedAt);
        // CreatedAt answers "when was this invitation first composed" and must survive edits.
        Assert.Equal(Now, settings.CreatedAt);
        // The template snapshot is untouched by a field-values-only update.
        Assert.Equal(TemplateId, settings.TemplateId);
    }

    [Fact]
    public void UpdateFieldValues_RejectsBlankInput()
    {
        var settings = Create();

        Assert.Throws<ArgumentException>(() => settings.UpdateFieldValues("", Now));
        Assert.Throws<ArgumentException>(() => settings.UpdateFieldValues("  ", Now));
    }

    [Fact]
    public void SnapshotTemplate_ReplacesTheWholeSnapshot()
    {
        var settings = Create();
        var otherTemplateId = Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444");
        var later = Now.AddDays(2);

        settings.SnapshotTemplate(otherTemplateId, 3, "<html>v3</html>", "body{}", "console.log(1)", later);

        Assert.Equal(otherTemplateId, settings.TemplateId);
        Assert.Equal(3, settings.TemplateVersion);
        Assert.Equal("<html>v3</html>", settings.HtmlContent);
        Assert.Equal("body{}", settings.CssContent);
        Assert.Equal("console.log(1)", settings.JsContent);
        Assert.Equal(later, settings.UpdatedAt);
    }

    [Fact]
    public void SnapshotTemplate_RejectsAnEmptyTemplateIdOrBlankHtml()
    {
        var settings = Create();

        Assert.Throws<ArgumentException>(() =>
            settings.SnapshotTemplate(Guid.Empty, 1, "<html></html>", null, null, Now));
        Assert.Throws<ArgumentException>(() =>
            settings.SnapshotTemplate(TemplateId, 1, " ", null, null, Now));
    }

    [Fact]
    public void EnablePublicLink_MintsATokenOnlyTheFirstTime()
    {
        var settings = Create();
        var generatedTokens = new Queue<string>(["token-one", "token-two"]);

        settings.EnablePublicLink(() => generatedTokens.Dequeue(), Now);
        var firstToken = settings.PublicEventToken;

        settings.DisablePublicLink(Now);
        settings.EnablePublicLink(() => generatedTokens.Dequeue(), Now);

        // Re-enabling must not silently invalidate a URL an organiser already shared.
        Assert.Equal(firstToken, settings.PublicEventToken);
        Assert.True(settings.PublicLinkEnabled);
    }

    [Fact]
    public void RevokePublicLink_RotatesTheToken()
    {
        var settings = Create();
        settings.EnablePublicLink(() => "original-token", Now);

        settings.RevokePublicLink("rotated-token", Now);

        Assert.Equal("rotated-token", settings.PublicEventToken);
        Assert.NotEqual("original-token", settings.PublicEventToken);
    }

    [Fact]
    public void IssuePreviewToken_OverwritesAnyPreviousToken()
    {
        var settings = Create();
        var expiresAt = Now.AddMinutes(15);

        settings.IssuePreviewToken("first-preview-token", expiresAt, Now);
        settings.IssuePreviewToken("second-preview-token", expiresAt.AddMinutes(15), Now);

        Assert.Equal("second-preview-token", settings.PreviewToken);
    }

    [Fact]
    public void TimestampsAreNormalisedToUtc()
    {
        var local = new DateTimeOffset(2026, 8, 12, 16, 0, 0, TimeSpan.FromHours(7));

        var settings = EventInvitationSettings.Create(
            EventId, TenantId, "{}", TemplateId, 1, "<html></html>", null, null, local);

        Assert.Equal(TimeSpan.Zero, settings.CreatedAt.Offset);
        Assert.Equal(local.ToUniversalTime(), settings.CreatedAt);
    }

    private static EventInvitationSettings Create(
        string fieldValues = "{\"brideName\":\"Amara\"}") =>
        EventInvitationSettings.Create(
            EventId, TenantId, fieldValues, TemplateId, 1, "<html></html>", null, null, Now);
}
