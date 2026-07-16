namespace GuestManagementService.Application.Guests;

public interface IGuestMetadataMapperFactory
{
    /// <summary>
    /// Resolves the mapper registered for <paramref name="eventType"/>, or <c>null</c> when the
    /// event type is unknown/unset — callers must not assume a mapper exists.
    /// </summary>
    IGuestMetadataMapper? Resolve(string? eventType);
}
