using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using Rehi.Application;
using Rehi.Application.Abstraction.Clock;
using Rehi.Domain.Common;
using Rehi.Infrastructure.Database;
using Rehi.Infrastructure.Serialization;

namespace Rehi.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class ProcessOutboxJob(
    ApplicationDbContext dbContext,
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider dateTimeProvider,
    IOptions<OutboxOptions> outboxOptions,
    ILogger<ProcessOutboxJob> logger) : IJob
{
    private const string ModuleName = "REHI";

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("{Module} - Beginning to process outbox messages", ModuleName);

        // await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();
        // await using DbTransaction transaction = await connection.BeginTransactionAsync();

        var outboxMessages = await GetOutboxMessagesAsync();

        foreach (var outboxMessage in outboxMessages)
        {
            Exception? exception = null;

            try
            {
                var domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    outboxMessage.Content,
                    SerializerSettings.Instance)!;

                using var scope = serviceScopeFactory.CreateScope();

                var handlers = DomainEventHandlersFactory.GetHandlers(
                    domainEvent.GetType(),
                    scope.ServiceProvider,
                    AssemblyReference.Assembly);

                foreach (var domainEventHandler in handlers)
                    await domainEventHandler.Handle(domainEvent, context.CancellationToken);
            }
            catch (Exception caughtException)
            {
                logger.LogError(
                    caughtException,
                    "{Module} - Exception while processing outbox message {MessageId}",
                    ModuleName,
                    outboxMessage.Id);

                exception = caughtException;
            }

            await UpdateOutboxMessageAsync(outboxMessage, exception);
        }

        // await transaction.CommitAsync();

        logger.LogInformation("{Module} - Completed processing outbox messages", ModuleName);
    }

    private async Task<IReadOnlyList<OutboxMessageResponse>> GetOutboxMessagesAsync()
    {
//         string sql =
//             $"""
//              SELECT
//                 id AS {nameof(OutboxMessageResponse.Id)},
//                 content AS {nameof(OutboxMessageResponse.Content)}
//              FROM academic.outbox_messages
//              WHERE processed_on_utc IS NULL
//              ORDER BY occurred_on_utc
//              LIMIT {outboxOptions.Value.BatchSize}
//              FOR UPDATE
//              """;
//
//         IEnumerable<OutboxMessageResponse> outboxMessages = await connection.QueryAsync<OutboxMessageResponse>(
//             sql,
//             transaction: transaction);
//
//         return outboxMessages.ToList();
        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(outboxOptions.Value.BatchSize)
            .Select(m => new OutboxMessageResponse
            {
                Id = m.Id,
                Content = m.Content
            })
            .ToListAsync();

        return messages;
    }

    private async Task UpdateOutboxMessageAsync(OutboxMessageResponse outboxMessage,
        Exception? exception)
    {
        // const string sql =
        //     """
        //     UPDATE academic.outbox_messages
        //     SET processed_on_utc = @ProcessedOnUtc,
        //         error = @Error
        //     WHERE id = @Id
        //     """;
        //
        // await connection.ExecuteAsync(
        //     sql,
        //     new
        //     {
        //         outboxMessage.Id,
        //         ProcessedOnUtc = dateTimeProvider.UtcNow,
        //         Error = exception?.ToString()
        //     },
        //     transaction: transaction);


        var message = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == outboxMessage.Id);

        if (message != null)
        {
            message.ProcessedOnUtc = dateTimeProvider.UtcNow;
            message.Error = exception?.ToString();

            await dbContext.SaveChangesAsync();
        }
    }

    internal sealed record OutboxMessageResponse
    {
        public Guid Id { get; init; }
        public string Content { get; init; } = null!;
    }
}