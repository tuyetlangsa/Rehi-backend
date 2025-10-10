using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Email;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rehi.Application.Email.SendEmail
{
    public abstract class SendEmail
    {
        public record Command(string userEmail, DateTime scheduleTime) : ICommand<Response>;

        public record Response(string userEmail, DateTime scheduleTime);

        internal class Handler : ICommandHandler<Command, Response>
        {
            private readonly IDbContext _dbContext;
            private readonly IUserContext _userContext;
            private readonly ISendEmailService _sendEmailService;

            public Handler(IDbContext dbContext, IUserContext userContext, ISendEmailService sendEmailService)
            {
                _dbContext = dbContext;
                _userContext = userContext;
                _sendEmailService = sendEmailService;
            }

            public async Task<Result<Response>> Handle(Command command, CancellationToken cancellationToken)
            {
                var email = command.userEmail ?? _userContext.Email;

                var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

                if (user is null)
                {
                    return Result.Failure<Response>(UserErrors.NotFound);
                }

                user.ScheduleTime = command.scheduleTime;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await _sendEmailService.ScheduleReminder(email, command.scheduleTime);
                return Result.Success(new Response(user.Email, user.ScheduleTime));
            }
        }
    }
}
