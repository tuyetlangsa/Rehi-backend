using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Quartz;
using Rehi.Application.Abstraction.Email;
using Rehi.Infrastructure.EmailService;

public class SendEmailService : ISendEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendEmailService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    public SendEmailService(IConfiguration configuration, ILogger<SendEmailService> logger, ISchedulerFactory schedulerFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _schedulerFactory = schedulerFactory;
    }

    public async Task SendEmailAsync(string to, string body)
    {
        string smtpUser = _configuration["SendEmail:SmtpUser"];
        string smtpPass = _configuration["SendEmail:SmtpPass"];

        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
        {
            _logger.LogError("SMTP credentials are missing in configuration (SendEmail:SmtpUser or SendEmail:SmtpPass).");
            throw new InvalidOperationException("SMTP credentials not found in configuration.");
        }

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(smtpUser));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = "Long oi Long dit me may";
        email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

        try
        {
            using var smtp = new SmtpClient();

            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(smtpUser, smtpPass);

            await smtp.SendAsync(email);
            _logger.LogInformation("Email sent successfully to {Recipient} with subject '{Subject}'.", to, "Long oi Long dit me may");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}. Error: {Message}", to, ex.Message);
            throw;
        }
        finally
        {
            _logger.LogInformation("Disconnecting from SMTP server...");
            try
            {
                using var smtp = new SmtpClient();
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred while disconnecting from SMTP server.");
            }
        }
    }

    public async Task ScheduleReminder(string userEmail, DateTime scheduledTime)
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        var jobKey = new JobKey(EmailReminderJob.Name);

        var trigger = TriggerBuilder.Create()
            .ForJob(jobKey)  
            .WithIdentity($"trigger-{Guid.NewGuid()}")
            .UsingJobData("userEmail", userEmail)
            .UsingJobData("message", "Long oi Long dit me may")
            .StartAt(scheduledTime)
            .Build();

        await scheduler.ScheduleJob(trigger);  
    }
}
