using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Articles.UpdateArticleLocation;

public abstract class UpdateArticleLocation
{
    public record Command(Guid ArticleId, string Location, long UpdateAt) : ICommand;

    internal class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail, cancellationToken);

            if (user is null) return Result.Failure(UserErrors.NotFound);

            var articleExisted = await dbContext.Articles
                .FirstOrDefaultAsync(a => command.ArticleId == a.Id, cancellationToken);

            if (articleExisted is null) return Result.Failure(ArticleErrors.NotFound);
            var updateAt = DateTimeOffset.FromUnixTimeMilliseconds(command.UpdateAt);
            
            if (updateAt < articleExisted.UpdateAt)
            {
                return Result.Failure(CommonErrors.StaleRequest);
            }
            
            articleExisted.Location = command.Location switch
            {
                "reading" => Location.Reading,
                "later" => Location.Later,
                "archived" => Location.Archived,
                "trash" => Location.Trash,
                _ => throw new ArgumentOutOfRangeException()
            };
            articleExisted.UpdateAt = updateAt;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ArticleId).NotEmpty();
            RuleFor(x => x.Location).NotEmpty();
        }
    }
}