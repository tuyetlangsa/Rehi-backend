using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.Refresh;

public abstract class Refresh
{
    public record Command(string RefreshToken) : ICommand<TokenPair>;

    internal sealed class Handler(IDbContext dbContext, ITokenIssuer tokenIssuer, IDateTimeProvider clock)
        : ICommandHandler<Command, TokenPair>
    {
        public Task<Result<TokenPair>> Handle(Command command, CancellationToken cancellationToken) =>
            dbContext.InSerializableTransactionAsync(() => Rotate(command.RefreshToken, cancellationToken),
                cancellationToken);

        private async Task<Result<TokenPair>> Rotate(string refreshToken, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var hash = OpaqueToken.HashOf(refreshToken);
            var token = await dbContext.RefreshTokens
                .Include(t => t.User)
                .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
            if (token is null) return Result.Failure<TokenPair>(AuthErrors.InvalidRefreshToken);

            switch (Sessions.StateOf(token, now))
            {
                case RefreshTokenState.Active:
                {
                    var (successor, pair) = Sessions.Issue(dbContext, tokenIssuer, token.User, token.Client,
                        token.FamilyId, now);
                    token.RevokedAt = now;
                    token.ReplacedById = successor.Id;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return pair;
                }
                case RefreshTokenState.RotatedWithinGrace
                    when !await Sessions.IsFamilyRevoked(dbContext, token.FamilyId, cancellationToken):
                {
                    var (_, pair) = Sessions.Issue(dbContext, tokenIssuer, token.User, token.Client,
                        token.FamilyId, now);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return pair;
                }
                case RefreshTokenState.Reused:
                    await Sessions.RevokeFamily(dbContext, token.FamilyId, now, cancellationToken);
                    return Result.Failure<TokenPair>(AuthErrors.InvalidRefreshToken);
                default:
                    return Result.Failure<TokenPair>(AuthErrors.InvalidRefreshToken);
            }
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
