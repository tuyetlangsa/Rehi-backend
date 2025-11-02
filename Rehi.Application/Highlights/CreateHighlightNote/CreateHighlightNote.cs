using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Highlights;
using Rehi.Domain.Users;

namespace Rehi.Application.Highlights.CreateHighlightNote;

public abstract class CreateHighlightNote
{
    public record Command(Guid HighlightId, string? Note, long SavedAt) : ICommand;


    internal class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail, cancellationToken);

            if (user is null) return Result.Failure(UserErrors.NotFound);

            var highlight = await dbContext.Highlights
                .SingleOrDefaultAsync(a => a.Id == command.HighlightId, cancellationToken);

            if (highlight is null) return Result.Failure(HighlightErrors.NotFound);
            var createAt = DateTimeOffset.FromUnixTimeMilliseconds(command.SavedAt);

            highlight!.Note = command.Note?.Trim();
            highlight!.UpdateAt = createAt;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.HighlightId).NotEmpty();
            RuleFor(x => x.SavedAt).NotEmpty();
        }
    }
}