using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.VerifyEmail;

public abstract class VerifyEmail
{
    public record Command(string Token) : ICommand;

    internal sealed class Handler(IDbContext dbContext, IDateTimeProvider clock) : ICommandHandler<Command>
    {
        public Task<Result> Handle(Command command, CancellationToken cancellationToken) =>
            dbContext.InSerializableTransactionAsync(async () =>
            {
                var now = clock.UtcNow;
                var hash = OpaqueToken.HashOf(command.Token);
                var token = await dbContext.EmailTokens
                    .Include(t => t.User)
                    .SingleOrDefaultAsync(t => t.TokenHash == hash && t.Purpose == EmailTokenPurpose.VerifyEmail,
                        cancellationToken);

                if (token is null || token.ConsumedAt is not null || token.ExpiresAt <= now)
                    return Result.Failure(AuthErrors.InvalidEmailToken);

                token.ConsumedAt = now;
                token.User.EmailVerifiedAt ??= now;
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }, cancellationToken);
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty();
        }
    }
}
