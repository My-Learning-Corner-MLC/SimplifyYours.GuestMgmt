using FluentValidation;
using FluentValidation.Results;

namespace GuestManagementService.Application.Guests;

/// <summary>
/// Resolves the <see cref="IGuestMetadataMapper"/> registered for an event type. Mappers are
/// discovered from DI registration (<c>IEnumerable&lt;IGuestMetadataMapper&gt;</c>) — adding a
/// new event type's mapper only requires registering it; this factory needs no changes.
/// </summary>
public sealed class GuestMetadataMapperFactory : IGuestMetadataMapperFactory
{
    private readonly IReadOnlyDictionary<string, IGuestMetadataMapper> mappersByEventType;

    public GuestMetadataMapperFactory(IEnumerable<IGuestMetadataMapper> mappers)
    {
        mappersByEventType = mappers.ToDictionary(
            mapper => mapper.EventType,
            StringComparer.OrdinalIgnoreCase);
    }

    public IGuestMetadataMapper Resolve(string eventType)
    {
        if (!string.IsNullOrWhiteSpace(eventType) && mappersByEventType.TryGetValue(eventType, out var mapper))
        {
            return mapper;
        }

        var supportedEventTypes = string.Join(", ", mappersByEventType.Keys.OrderBy(type => type));
        throw new ValidationException(new[]
        {
            new ValidationFailure(
                "EventType",
                $"Guests cannot be added to '{eventType}' events yet. Supported event types: {supportedEventTypes}."),
        });
    }
}
