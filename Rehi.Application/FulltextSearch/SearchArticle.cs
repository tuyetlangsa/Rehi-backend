using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.FulltextSearch;

public abstract class SearchArticle
{
    public record Query(string QueryText): IQuery<List<Guid>>;
    
    internal class Handler(IDbContext dbContext, IUserContext userContext) : IQueryHandler<Query, List<Guid>>
    {
        public async Task<Result<List<Guid>>> Handle(Query query, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail , cancellationToken);

            if (user is null)
            {
                return Result.Failure<List<Guid>>(UserErrors.NotFound);
            }

            if (string.IsNullOrEmpty(query.QueryText))
            {
                return new List<Guid>();
            }
            
            var articleIds = await dbContext.Articles
                .Where(a => a.UserId == user.Id)
                .Where(a => EF.Functions.ToTsVector("english", a.Title + " " + a.Author + " " + a.TextContent)
                    .Matches(EF.Functions.PlainToTsQuery("english", query.QueryText)))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);
            
            return articleIds;
        }
    }
}