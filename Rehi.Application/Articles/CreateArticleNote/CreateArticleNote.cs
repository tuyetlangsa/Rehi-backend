using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Articles.CreateArticleNote;

public abstract class CreateArticleNote
{
    public record Command(Guid ArticleId, string? Note, long SavedAt): ICommand;
    

    internal class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail , cancellationToken);

            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound);
            }
            
            var articleExisted = await dbContext.Articles
                .SingleOrDefaultAsync(a => a.Id == command.ArticleId, cancellationToken);

            if (articleExisted is null)
            {
                return Result.Failure(ArticleErrors.NotFound);
            }
            var createAt = DateTimeOffset.FromUnixTimeMilliseconds(command.SavedAt);

            articleExisted!.Note = command.Note?.Trim();
            articleExisted!.UpdateAt = createAt;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
    
    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ArticleId).NotEmpty();
            RuleFor(x => x.SavedAt).NotEmpty();
        }
    }
}