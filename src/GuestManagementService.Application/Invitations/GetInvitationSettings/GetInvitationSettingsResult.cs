namespace GuestManagementService.Application.Invitations.GetInvitationSettings;

public sealed record GetInvitationSettingsResult(
    GetInvitationSettingsStatus Status,
    string EventType = "",
    string? TemplateId = null,
    string? TemplateName = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null,
    bool IsConfigured = false,
    bool PublicLinkEnabled = false,
    string? PublicEventToken = null)
{
    public static GetInvitationSettingsResult EventNotFound() => new(GetInvitationSettingsStatus.EventNotFound);
}
