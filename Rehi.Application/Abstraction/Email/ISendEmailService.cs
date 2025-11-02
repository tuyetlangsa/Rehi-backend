namespace Rehi.Application.Abstraction.Email;

public interface ISendEmailService
{
    Task ScheduleReminder(string userEmail, DateTime scheduledTime);
    Task SendEmailAsync(string userEmail, string body);
}