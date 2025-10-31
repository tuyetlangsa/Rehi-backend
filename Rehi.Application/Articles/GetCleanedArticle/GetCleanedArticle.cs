using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Articles.GetCleanedArticle;

public abstract class GetCleanedArticle
{
    public record Query(Guid ArticleId) : IQuery<string>;

    internal sealed class Handler(IDbContext dbContext, IUserContext userContext) : IQueryHandler<Query, string>
    {
        public async Task<Result<string>> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userContext.Email, cancellationToken);

            if (user is null) return Result.Failure<string>(UserErrors.NotFound);


            var article = await dbContext.Articles.FindAsync(request.ArticleId, cancellationToken);

            if (article is null) return Result.Failure<string>(ArticleErrors.NotFound);

            return article.Content;
        }
    }
}