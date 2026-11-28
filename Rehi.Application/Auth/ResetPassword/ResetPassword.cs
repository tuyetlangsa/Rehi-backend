using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.ResetPassword;

public abstract class ResetPassword
{
    public record Command(string Token, string NewPassword) : ICommand;

    internal sealed class Handler(IDbContext dbContext, IPasswordHasher passwordHasher, IDateTimeProvider clock)
        : ICommandHandler<Command>
    {
        public Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var passwordHash = passwordHasher.Hash(command.NewPassword);
            return dbContext.InSerializableTransactionAsync(async () =>
            {
                var now = clock.UtcNow;
                var hash = OpaqueToken.HashOf(command.Token);
                var token = await dbContext.EmailTokens
                    .Include(t => t.User)
                    .SingleOrDefaultAsync(t => t.TokenHash == hash && t.Purpose == EmailTokenPurpose.ResetPassword,
                        cancellationToken);

                if (token is null || token.ConsumedAt is not null || token.ExpiresAt <= now)
                    return Result.Failure(AuthErrors.InvalidEmailToken);

                token.ConsumedAt = now;
                token.User.PasswordHash = passwordHash;
                token.User.EmailVerifiedAt ??= now;

                var active = await dbContext.RefreshTokens
                    .Where(t => t.UserId == token.UserId && t.RevokedAt == null)
                    .ToListAsync(cancellationToken);
                foreach (var refreshToken in active) refreshToken.RevokedAt = now;

                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }, cancellationToken);
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().Length(8, 128);
        }
    }
}
