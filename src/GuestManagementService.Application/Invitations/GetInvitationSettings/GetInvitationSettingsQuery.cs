using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Invitations.GetInvitationSettings;

public sealed record GetInvitationSettingsQuery(Guid EventId) : BaseCommand, IRequest<GetInvitationSettingsResult>;
