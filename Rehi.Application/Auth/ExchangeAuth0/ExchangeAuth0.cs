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
            var user = await Find(email, cancellationToken) ?? await Create(email, command.Name, now, cancellationToken);
            if (user.EmailVerifiedAt is null)
            {
                // Whoever set a password on this unverified row never proved they own the address. Auth0 did.
                user.PasswordHash = null;
                await PendingEmailTokens.VoidAsync(dbContext, user.Id, EmailTokenPurpose.VerifyEmail, now,
                    cancellationToken);
            }

            var session = Sessions.Issue(dbContext, tokenIssuer, user, RefreshTokenClient.Web, Guid.NewGuid(), now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return session.Pair;
        }

        private Task<User?> Find(string email, CancellationToken cancellationToken) =>
            dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        private async Task<User> Create(string email, string? name, DateTime now, CancellationToken cancellationToken)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim(),
                CreateAt = now,
                ScheduleTime = now
            };
            dbContext.Users.Add(user);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return user;
            }
            catch (DbUpdateException)
            {
                // IX_Users_Email rejected the insert because a concurrent first exchange of this email committed first.
                // Remove detaches the failed insert, so the session save below does not retry it.
                dbContext.Users.Remove(user);
                if (await Find(email, cancellationToken) is { } winner) return winner;
                throw;
            }
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
