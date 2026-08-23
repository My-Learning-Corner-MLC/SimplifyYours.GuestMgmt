using GuestManagementService.Application.Authorization;
using MediatR;

namespace GuestManagementService.Application.Invitations.RotatePublicToken;

/// <summary>
/// Replaces the public event token so the previously shared URL immediately 404s, while the link
/// stays enabled at a new URL. Distinct from disabling: that turns the link off, this issues a
/// fresh one. Requires the link to already be enabled — rotating a disabled link is meaningless.
/// </summary>
public sealed record RotatePublicTokenCommand(Guid EventId) : BaseCommand, IRequest<RotatePublicTokenResult>;
