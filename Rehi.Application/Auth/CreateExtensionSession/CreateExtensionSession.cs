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
    /// <param name="SessionId">The refresh-token family named by the caller's access token (<c>sid</c>).</param>
    public record Command(Guid? SessionId) : ICommand<TokenPair>;

    internal sealed class Handler(
        IDbContext dbContext,
        ITokenIssuer tokenIssuer,
        IDateTimeProvider clock) : ICommandHandler<Command, TokenPair>
    {
        public async Task<Result<TokenPair>> Handle(Command command, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var user = await dbContext.RefreshTokens
                .Where(t => t.FamilyId == command.SessionId && t.RevokedAt == null && t.ExpiresAt > now)
                .Select(t => t.User)
                .FirstOrDefaultAsync(cancellationToken);
            if (user is null) return Result.Failure<TokenPair>(AuthErrors.SessionEnded);

            var session = Sessions.Issue(dbContext, tokenIssuer, user, RefreshTokenClient.Extension, Guid.NewGuid(), now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return session.Pair;
        }
    }
}
