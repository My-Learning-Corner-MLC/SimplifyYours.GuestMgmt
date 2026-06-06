namespace GuestManagementService.Contracts.IntegrationEvents;

public sealed record EventReferencePayload(
    Guid EventId,
    string EventName);
