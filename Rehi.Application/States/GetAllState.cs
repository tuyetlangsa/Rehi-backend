using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.States;

public class GetAllState
{
    public record Query() : IQuery<Response>;
    public record Response(
        List<TagResponse> Tags, 
        List<ArticleResponse> Articles);
    public record ArticleResponse(
        Guid Id,
        string Url,
        string? Title,
        string? Author,
        string? Summary,
        string? ImagePreviewUrl,
        string? TextContent,
        long CreateAt,
        long? UpdateAt,
        string? Language,
        int? WordCount,
        List<Guid> TagIds,
        TimeSpan? TimeToRead,
        string? CleanedHtml,
        bool IsDeleted);
    public record TagResponse(
        Guid Id,
        string Name,
        bool IsDeleted);
    
    
     internal sealed class Handler(IDbContext dbContext, IUserContext userContext) : IQueryHandler<Query, Response>
     {
         public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
         {
             var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userContext.Email);
             if (user is null)
             {
                 return Result.Failure<Response>(UserErrors.NotFound);
             }
             dbContext.Articles.IgnoreQueryFilters();

             var tagResponses = await dbContext.Tags
                 .AsNoTracking().Select(t => new TagResponse(t.Id, t.Name, t.IsDeleted))
                 .ToListAsync(cancellationToken);

             var articles = await dbContext.Articles
                 .AsNoTracking().ToListAsync(cancellationToken);
             
             var articleResponses = articles.Select(a =>
             {
                 var createAt = a.CreateAt.ToUnixTimeMilliseconds();
                 var updateAt = a.UpdateAt?.ToUnixTimeMilliseconds();
                 return new ArticleResponse(
                     a.Id,
                     a.Url,
                     a.Title,
                     a.Author,
                     a.Summary,
                     a.ImageUrl,
                     a.TextContent,
                     createAt,
                     updateAt,
                     a.Language,
                     a.WordCount,
                     a.Tags.Select(t => t.Id).ToList(),
                     a.TimeToRead,
                     a.Content,
                     a.IsDeleted);
             }).ToList();
             
             return new Response(tagResponses, articleResponses);
         }
     }
}