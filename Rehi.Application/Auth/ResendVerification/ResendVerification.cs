using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Auth.SendEmailToken;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.ResendVerification;

public abstract class ResendVerification
{
    public record Command(string Email) : ICommand;

    internal sealed class Handler(IDbContext dbContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);
            if (user is not { PasswordHash: not null, EmailVerifiedAt: null }) return Result.Success();

            user.Raise(new EmailTokenRequestedDomainEvent(user.Id, EmailTokenPurpose.VerifyEmail));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}
