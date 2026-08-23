namespace GuestManagementService.Application.Invitations.SaveInvitationSettings;

public sealed record SaveInvitationSettingsResult(
    SaveInvitationSettingsStatus Status,
    string EventType = "",
    string? TemplateId = null,
    string? TemplateName = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null,
    bool PublicLinkEnabled = false,
    string? PublicEventToken = null)
{
    public static SaveInvitationSettingsResult EventNotFound() => new(SaveInvitationSettingsStatus.EventNotFound);
}
