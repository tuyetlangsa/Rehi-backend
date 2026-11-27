using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;

namespace Rehi.Application.Auth.Logout;

public abstract class Logout
{
    public record Command(string RefreshToken) : ICommand;

    internal sealed class Handler(IDbContext dbContext, IDateTimeProvider clock) : ICommandHandler<Command>
    {
        public Task<Result> Handle(Command command, CancellationToken cancellationToken) =>
            dbContext.InSerializableTransactionAsync(async () =>
            {
                var hash = OpaqueToken.HashOf(command.RefreshToken);
                var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash,
                    cancellationToken);
                if (token is not null)
                    await Sessions.RevokeFamily(dbContext, token.FamilyId, clock.UtcNow, cancellationToken);
                return Result.Success();
            }, cancellationToken);
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
