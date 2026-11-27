using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.ExchangeAuth0;

public abstract class ExchangeAuth0
{
    public record Command(string? Email, string? Name, bool? EmailVerified) : ICommand<TokenPair>;

    internal sealed class Handler(
        IDbContext dbContext,
        ITokenIssuer tokenIssuer,
        IDateTimeProvider clock) : ICommandHandler<Command, TokenPair>
    {
        public async Task<Result<TokenPair>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (command.EmailVerified == false) return Result.Failure<TokenPair>(AuthErrors.EmailNotVerified);

            var email = Emails.Normalize(command.Email);
            var now = clock.UtcNow;
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FullName = string.IsNullOrWhiteSpace(command.Name) ? email.Split('@')[0] : command.Name.Trim(),
                    CreateAt = now,
                    ScheduleTime = now
                };
                dbContext.Users.Add(user);
            }

            var session = Sessions.Issue(dbContext, tokenIssuer, user, RefreshTokenClient.Web, Guid.NewGuid(), now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return session.Pair;
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty();
        }
    }
}
