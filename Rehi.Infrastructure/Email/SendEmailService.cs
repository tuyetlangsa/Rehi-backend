using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using Quartz;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Email;
using Rehi.Infrastructure.EmailService;

public class SendEmailService : ISendEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendEmailService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IDbContext _dbContext;
    public SendEmailService(IConfiguration configuration, ILogger<SendEmailService> logger,
        ISchedulerFactory schedulerFactory, IDbContext dbContext)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _schedulerFactory = schedulerFactory;
    }

    public async Task SendEmailAsync(string to, string body, string subject = "Rehi Email Service")
    {
        var smtpUser = _configuration["SendEmail:SmtpUser"];
        var smtpPass = _configuration["SendEmail:SmtpPass"];

        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
        {
            _logger.LogError(
                "SMTP credentials are missing in configuration (SendEmail:SmtpUser or SendEmail:SmtpPass).");
            throw new InvalidOperationException("SMTP credentials not found in configuration.");
        }

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(smtpUser));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        try
        {
            using var smtp = new SmtpClient();

            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(smtpUser, smtpPass);

            await smtp.SendAsync(email);
            _logger.LogInformation("Email sent successfully to {Recipient} with subject '{Subject}'.", to,
                subject);
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

    public async Task ScheduleReminder(string userEmail, Guid userId, DateTime scheduledTime)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        
        var jobKey = new JobKey(EmailReminderJob.Name);
        var today = DateTime.UtcNow.Date;

        var flashcards = await _dbContext.Flashcards
            .Include(fc => fc.Highlight)
            .ThenInclude(h => h.User)
            .Where(fc => fc.Highlight.UserId == userId && fc.DueDate == today)
            .ToListAsync();
        
        var message = flashcards.Any()
            ? $"You have {flashcards.Count} flashcards due today:\n" +
              string.Join("\n", flashcards.Select(fc => $"- {fc.Highlight.PlainText}"))
            : "No flashcards due today. Keep learning!";        
        
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