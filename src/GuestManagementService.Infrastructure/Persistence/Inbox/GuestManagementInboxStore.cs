using Microsoft.EntityFrameworkCore;
using SimplifyYours.Event.Abstractions;

namespace GuestManagementService.Infrastructure.Persistence.Inbox;

internal sealed class GuestManagementInboxStore(GuestManagementServiceDbContext dbContext) : IEventInboxStore
{
    public async Task<EventInboxRecord?> GetAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return await dbContext.InboxMessages
            .AsNoTracking()
            .Where(message => message.Id == messageId)
            .Select(message => new EventInboxRecord(
                message.Id,
                message.EventType,
                message.ReceivedAt,
                message.ProcessedAt,
                message.HandleAttempts,
                message.Status,
                message.Error))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task MarkProcessingAsync(
        IntegrationEventEnvelope envelope,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.InboxMessages.SingleOrDefaultAsync(
            inboxMessage => inboxMessage.Id == envelope.EventId,
            cancellationToken);

        if (message is null)
        {
            await dbContext.InboxMessages.AddAsync(
                InboxMessage.Processing(envelope.EventId, envelope.EventType, receivedAt),
                cancellationToken);
        }
        else
        {
            message.MarkProcessing();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(
        Guid messageId,
        string eventType,
        DateTimeOffset processedAt,
        CancellationToken cancellationToken)
    {
        var message = await GetOrCreateAsync(messageId, eventType, cancellationToken);
        message.MarkProcessed(processedAt);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string eventType,
        string error,
        bool terminal,
        CancellationToken cancellationToken)
    {
        var message = await GetOrCreateAsync(messageId, eventType, cancellationToken);
        message.MarkFailed(error, terminal);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<InboxMessage> GetOrCreateAsync(
        Guid messageId,
        string eventType,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.InboxMessages.SingleOrDefaultAsync(
            inboxMessage => inboxMessage.Id == messageId,
            cancellationToken);

        if (message is not null)
        {
            return message;
        }

        message = InboxMessage.Processing(messageId, eventType, DateTimeOffset.UtcNow);
        await dbContext.InboxMessages.AddAsync(message, cancellationToken);

        return message;
    }
}
