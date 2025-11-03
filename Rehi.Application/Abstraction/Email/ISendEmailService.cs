namespace Rehi.Application.Abstraction.Email;

public interface ISendEmailService
{
    Task ScheduleReminder(string userEmail, Guid userId, DateTime scheduledTime);
    Task SendEmailAsync(string to, string body, string subject);
}