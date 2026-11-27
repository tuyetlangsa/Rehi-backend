using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Auth.SendEmailToken;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.Register;

public abstract class Register
{
    public record Command(string Email, string Password, string FullName) : ICommand;

    internal sealed class Handler(IDbContext dbContext, IPasswordHasher passwordHasher, IDateTimeProvider clock)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var holders = await dbContext.Users
                .Where(u => u.Email.ToLower() == command.Email)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            return holders switch
            {
                [] => await Create(command, cancellationToken),
                [var userId] => await ReplaceUnverifiedPassword(userId, command, cancellationToken),
                _ => Result.Failure(UserErrors.EmailAlreadyExists)
            };
        }

        private async Task<Result> Create(Command command, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                FullName = command.FullName,
                PasswordHash = passwordHasher.Hash(command.Password),
                CreateAt = now,
                ScheduleTime = now
            };
            user.Raise(new EmailTokenRequestedDomainEvent(user.Id, EmailTokenPurpose.VerifyEmail));
            dbContext.Users.Add(user);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (DbUpdateException)
            {
                // IX_Users_Email rejected the insert because a concurrent register of this email committed first.
                if (!await dbContext.Users.AnyAsync(u => u.Email == command.Email, cancellationToken)) throw;
                return Result.Failure(UserErrors.EmailAlreadyExists);
            }
        }

        private Task<Result> ReplaceUnverifiedPassword(Guid userId, Command command,
            CancellationToken cancellationToken)
        {
            var passwordHash = passwordHasher.Hash(command.Password);
            return dbContext.InSerializableTransactionAsync(async () =>
            {
                var user = await dbContext.Users.SingleAsync(u => u.Id == userId, cancellationToken);
                if (user is not { PasswordHash: not null, EmailVerifiedAt: null })
                    return Result.Failure(UserErrors.EmailAlreadyExists);

                var now = clock.UtcNow;
                user.PasswordHash = passwordHash;
                user.FullName = command.FullName;
                await PendingEmailTokens.VoidAsync(dbContext, user.Id, EmailTokenPurpose.VerifyEmail, now,
                    cancellationToken);
                user.Raise(new EmailTokenRequestedDomainEvent(user.Id, EmailTokenPurpose.VerifyEmail));
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }, cancellationToken);
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().Length(8, 128);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        }
    }
}
