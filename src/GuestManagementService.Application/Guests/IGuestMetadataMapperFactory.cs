namespace GuestManagementService.Application.Guests;

public interface IGuestMetadataMapperFactory
{
    /// <summary>
    /// Resolves the mapper registered for <paramref name="eventType"/>. Throws
    /// <see cref="FluentValidation.ValidationException"/> when no mapper is registered for it —
    /// callers can rely on the result always being present.
    /// </summary>
    IGuestMetadataMapper Resolve(string eventType);
}
