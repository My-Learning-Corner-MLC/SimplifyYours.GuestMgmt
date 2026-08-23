using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Invitations.SaveInvitationSettings;

/// <param name="PublicLinkEnabled">
/// Omitted (null) leaves the public link untouched. <see langword="true"/> enables it, minting a
/// token if none exists. <see langword="false"/> disables it AND revokes the token — see
/// <c>EventInvitationSettings.DisablePublicLink</c>.
/// </param>
public sealed record SaveInvitationSettingsCommand(
    Guid EventId,
    string? TemplateId,
    IReadOnlyDictionary<string, string?>? FieldValues,
    bool? PublicLinkEnabled = null) : BaseCommand, IRequest<SaveInvitationSettingsResult>;
