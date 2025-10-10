using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rehi.Application.Abstraction.Email
{
    public interface ISendEmailService
    {
        Task ScheduleReminder(string userEmail, DateTime scheduledTime);
        Task SendEmailAsync(string userEmail, string body);
    }
}
