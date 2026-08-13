using GuestManagementService.Domain.Invitations;

namespace GuestManagementService.UnitTests.Invitations;

public sealed class EventInvitationSettingsTests
{
    private static readonly Guid EventId = Guid.Parse("6f9b3c2a-6d1e-4f5b-9c3a-2e7d8b1f4a55");
    private static readonly Guid TenantId = Guid.Parse("0fa219ed-70ad-4e8d-9f51-6e60409dc659");
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StoresTheTemplateAndItsValues()
    {
        var settings = Create();

        Assert.Equal(EventId, settings.EventId);
        Assert.Equal(TenantId, settings.TenantId);
        Assert.Equal("marigold", settings.TemplateId);
        Assert.Equal("{\"brideName\":\"Amara\"}", settings.FieldValues);
        Assert.Equal(Now, settings.CreatedAt);
        Assert.Equal(Now, settings.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsABlankTemplateId(string templateId)
    {
        // Settings with no template describe an invitation that cannot render. Fail at construction
        // rather than surface a blank page to a guest.
        Assert.Throws<ArgumentException>(() => Create(templateId: templateId));
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
            EventInvitationSettings.Create(Guid.Empty, TenantId, "marigold", "{}", Now));
        Assert.Throws<ArgumentException>(() =>
            EventInvitationSettings.Create(EventId, Guid.Empty, "marigold", "{}", Now));
    }

    [Fact]
    public void Update_ReplacesContentAndMovesUpdatedAtOnly()
    {
        var settings = Create();
        var later = Now.AddDays(2);

        settings.Update("verona", "{\"brideName\":\"Priya\"}", later);

        Assert.Equal("verona", settings.TemplateId);
        Assert.Equal("{\"brideName\":\"Priya\"}", settings.FieldValues);
        Assert.Equal(later, settings.UpdatedAt);
        // CreatedAt answers "when was this invitation first composed" and must survive edits.
        Assert.Equal(Now, settings.CreatedAt);
    }

    [Fact]
    public void Update_RejectsBlankInput()
    {
        var settings = Create();

        Assert.Throws<ArgumentException>(() => settings.Update("", "{}", Now));
        Assert.Throws<ArgumentException>(() => settings.Update("marigold", "  ", Now));
    }

    [Fact]
    public void TimestampsAreNormalisedToUtc()
    {
        var local = new DateTimeOffset(2026, 8, 12, 16, 0, 0, TimeSpan.FromHours(7));

        var settings = EventInvitationSettings.Create(EventId, TenantId, "marigold", "{}", local);

        Assert.Equal(TimeSpan.Zero, settings.CreatedAt.Offset);
        Assert.Equal(local.ToUniversalTime(), settings.CreatedAt);
    }

    private static EventInvitationSettings Create(
        string templateId = "marigold",
        string fieldValues = "{\"brideName\":\"Amara\"}") =>
        EventInvitationSettings.Create(EventId, TenantId, templateId, fieldValues, Now);
}
