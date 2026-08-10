namespace GuestManagementService.Application.Abstractions.Guests;

/// <summary>
/// Mints the opaque token that identifies a guest's invitation link.
/// </summary>
/// <remarks>
/// The token is the only thing standing between an anonymous caller and a guest's personal data,
/// so it must come from a cryptographic RNG — never <c>Guid.NewGuid()</c>, whose value is not
/// guaranteed to be unpredictable, and never a sequential or derived value.
/// </remarks>
public interface IInvitationTokenGenerator
{
    string Generate();
}
