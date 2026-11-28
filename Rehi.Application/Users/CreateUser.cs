using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Auth;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Users;

public abstract class CreateUser
{
    public record Command(string FullName) : ICommand<Guid>;

    internal sealed class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var email = userContext.Email;
            var user = await Emails.FindUserAsync(dbContext, Emails.Normalize(email), cancellationToken);
            if (user is not null) return user.Id;

            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = request.FullName,
                CreateAt = DateTime.UtcNow,
                ScheduleTime = DateTime.UtcNow
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
    }
}
