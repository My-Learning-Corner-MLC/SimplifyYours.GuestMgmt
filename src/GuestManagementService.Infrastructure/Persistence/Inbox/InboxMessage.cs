using SimplifyYours.Event.Abstractions;

namespace GuestManagementService.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    private InboxMessage(Guid id, string eventType, DateTimeOffset receivedAt)
    {
        Id = id;
        EventType = eventType;
        ReceivedAt = receivedAt.ToUniversalTime();
        ProcessedAt = null;
        HandleAttempts = 0;
        Status = EventInboxRecordStatus.Processing;
        Error = null;
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int HandleAttempts { get; private set; }

    public EventInboxRecordStatus Status { get; private set; }

    public string? Error { get; private set; }

    public static InboxMessage Processing(Guid id, string eventType, DateTimeOffset receivedAt)
    {
        return new InboxMessage(id, eventType, receivedAt);
    }

    public void MarkProcessing()
    {
        Status = EventInboxRecordStatus.Processing;
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        Status = EventInboxRecordStatus.Processed;
        ProcessedAt = processedAt.ToUniversalTime();
        Error = null;
    }

    public void MarkFailed(string error, bool terminal)
    {
        HandleAttempts++;
        Status = terminal ? EventInboxRecordStatus.Failed : EventInboxRecordStatus.Processing;
        Error = error.Length > 2_000 ? error[..2_000] : error;
    }
}
