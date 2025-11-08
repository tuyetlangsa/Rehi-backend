using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Email;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;
using System.Globalization; // Needed for specific parsing

namespace Rehi.Application.Email.SendEmail;

public abstract class SendEmail
{
    // Keeping this as string based on your original code, assuming API input
    public record Command(string ScheduleTime) : ICommand<Response>; 

    // Assuming ScheduleTime on the user and response is DateTime
    public record Response(string UserEmail, DateTime ScheduleTime);

    internal class Handler : ICommandHandler<Command, Response>
    {
        private readonly IDbContext _dbContext;
        private readonly ISendEmailService _sendEmailService;
        private readonly IUserContext _userContext;

        public Handler(IDbContext dbContext, IUserContext userContext, ISendEmailService sendEmailService)
        {
            _dbContext = dbContext;
            _userContext = userContext;
            _sendEmailService = sendEmailService;
        }

        public async Task<Result<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            var email = _userContext.Email;

            // 1. Parse input string to DateTime with invariant culture
            if (!DateTime.TryParse(command.ScheduleTime, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out DateTime parsedScheduleTime))
            {
                return Result.Failure<Response>(UserErrors.NotFound);
            }

            // 2. Convert to UTC to satisfy PostgreSQL timestamptz
            parsedScheduleTime = parsedScheduleTime.ToUniversalTime();

            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user is null)
                return Result.Failure<Response>(UserErrors.NotFound);

            // 3. Assign UTC datetime to entity
            user.ScheduleTime = parsedScheduleTime;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 4. Call service with UTC time
            await _sendEmailService.ScheduleReminder(email, user.Id, parsedScheduleTime);

            return Result.Success(new Response(user.Email, user.ScheduleTime));
        }

    }
}