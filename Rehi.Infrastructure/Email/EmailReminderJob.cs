using Microsoft.Extensions.Logging;
using Quartz;
using Rehi.Application.Abstraction.Email;

namespace Rehi.Infrastructure.EmailService;

public class EmailReminderJob(ILogger<EmailReminderJob> logger, ISendEmailService emailService) : IJob
{
    public const string Name = nameof(EmailReminderJob);

    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.MergedJobDataMap;

        var userEmail = data.GetString("userEmail");
        //string? message = data.GetString("message");
        var messageHehe = "TEST TEST TEST TEST TEST";

        try
        {
            await emailService.SendEmailAsync(userEmail, messageHehe);

            logger.LogInformation("Send reminder to user {UserId}: {Message}", userEmail, messageHehe);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send reminder to user {UserId}", userEmail);
            throw;
        }
    }
}