using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.CreateExtensionSession;

public abstract class CreateExtensionSession
{
    public record Command : ICommand<TokenPair>;

    internal sealed class Handler(
        IDbContext dbContext,
        IUserContext userContext,
        ITokenIssuer tokenIssuer,
        IDateTimeProvider clock) : ICommandHandler<Command, TokenPair>
    {
        public async Task<Result<TokenPair>> Handle(Command command, CancellationToken cancellationToken)
        {
            var email = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user is null) return Result.Failure<TokenPair>(UserErrors.NotFound);

            var session = Sessions.Issue(dbContext, tokenIssuer, user, RefreshTokenClient.Extension, Guid.NewGuid(), clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return session.Pair;
        }
    }
}
