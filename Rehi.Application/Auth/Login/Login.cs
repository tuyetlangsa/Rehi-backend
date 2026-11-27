using FluentValidation;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.Login;

public abstract class Login
{
    public record Command(string Email, string Password, RefreshTokenClient? Client) : ICommand<TokenPair>;

    internal sealed class Handler(
        IDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IDateTimeProvider clock) : ICommandHandler<Command, TokenPair>
    {
        public async Task<Result<TokenPair>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await Emails.FindUserAsync(dbContext, command.Email, cancellationToken);
            var passwordMatches = passwordHasher.Verify(user?.PasswordHash, command.Password);
            if (user is null || !passwordMatches) return Result.Failure<TokenPair>(AuthErrors.InvalidCredentials);
            if (user.EmailVerifiedAt is null) return Result.Failure<TokenPair>(AuthErrors.EmailNotVerified);

            var session = Sessions.Issue(dbContext, tokenIssuer, user, command.Client!.Value, Guid.NewGuid(), clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return session.Pair;
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
            RuleFor(x => x.Client).NotNull().WithMessage("Client must be \"web\" or \"extension\".");
        }
    }
}
