using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Infrastructure.Database;

namespace Rehi.Infrastructure.Outbox;

internal sealed class IdempotentDomainEventHandler<TDomainEvent>(
    IDomainEventHandler<TDomainEvent> decorated,
    ApplicationDbContext dbContext)
    : DomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public override async Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {

        var outboxMessageConsumer = new OutboxMessageConsumer(domainEvent.Id, decorated.GetType().Name);

        if (await OutboxConsumerExistsAsync(outboxMessageConsumer))
        {
            return;
        }

        await decorated.Handle(domainEvent, cancellationToken);

        await InsertOutboxConsumerAsync(outboxMessageConsumer);
    }

    private async Task<bool> OutboxConsumerExistsAsync(
        OutboxMessageConsumer outboxMessageConsumer)
    {
        
        // const string sql = 
        //     """
        //     SELECT EXISTS(
        //         SELECT 1
        //         FROM events.outbox_message_consumers
        //         WHERE outbox_message_id = @OutboxMessageId AND
        //               name = @Name
        //     )
        //     """;
        //
        // return await dbConnection.ExecuteScalarAsync<bool>(sql, outboxMessageConsumer);
        
        return await dbContext.OutboxMessageConsumers
            .AnyAsync(c =>
                c.OutboxMessageId == outboxMessageConsumer.OutboxMessageId &&
                c.Name == outboxMessageConsumer.Name);
    }

    private async Task InsertOutboxConsumerAsync(
        OutboxMessageConsumer outboxMessageConsumer)
    {
        // const string sql =
        //     """
        //     INSERT INTO events.outbox_message_consumers(outbox_message_id, name)
        //     VALUES (@OutboxMessageId, @Name)
        //     """;
        //
        // await dbConnection.ExecuteAsync(sql, outboxMessageConsumer);
        dbContext.OutboxMessageConsumers.Add(outboxMessageConsumer);
        await dbContext.SaveChangesAsync();
    }
}
