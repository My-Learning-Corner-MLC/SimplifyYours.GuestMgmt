namespace GuestManagementService.Application.Guests;

/// <summary>
/// Resolves the <see cref="IGuestMetadataMapper"/> registered for an event type. Mappers are
/// discovered from DI registration (<c>IEnumerable&lt;IGuestMetadataMapper&gt;</c>) — adding a
/// new event type's mapper only requires registering it; this factory needs no changes.
/// </summary>
public sealed class GuestMetadataMapperFactory(IEnumerable<IGuestMetadataMapper> mappers)
    : IGuestMetadataMapperFactory
{
    public IGuestMetadataMapper? Resolve(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return null;
        }

        return mappers.FirstOrDefault(
            mapper => string.Equals(mapper.EventType, eventType, StringComparison.OrdinalIgnoreCase));
    }
}
