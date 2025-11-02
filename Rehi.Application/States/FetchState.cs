using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.States;

public abstract class FetchState
{
    public record Query(long LastUpdateTime) : IQuery<Response>;

    public record Response(Created Created, Updated Updated);

    public record Created(List<ArticleResponse> Articles, List<TagResponse> Tags, List<HighlightResponse> Highlights);

    public record Updated(List<ArticleResponse> Articles, List<TagResponse> Tags, List<HighlightResponse> Highlights);

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
        bool IsDeleted,
        string Location,
        string? Note);

    public record TagResponse(
        Guid Id,
        string Name,
        bool IsDeleted);

    public record HighlightResponse(
        Guid Id,
        Guid ArticleId,
        string Location,
        string Html,
        string Markdown,
        string PlainText,
        long CreateAt,
        long? UpdateAt,
        string? Color,
        bool IsDeleted,
        string CreateBy,
        string? Note);

    internal sealed class Handler(IDbContext dbContext, IUserContext userContext) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userContext.Email);
            if (user is null) return Result.Failure<Response>(UserErrors.NotFound);

            var lastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(request.LastUpdateTime);

            var tags = await dbContext.Tags.AsNoTracking()
                .Where(t => (t.CreateAt > lastUpdateTime
                             || t.UpdateAt > lastUpdateTime) && t.UserId == user.Id)
                .ToListAsync(cancellationToken);

            var createdTags = tags
                .Where(t => t.CreateAt > lastUpdateTime)
                .Select(t => new TagResponse(t.Id, t.Name, t.IsDeleted))
                .ToList();

            var updatedTags = tags
                .Where(t => t.CreateAt <= lastUpdateTime
                            && t.UpdateAt != null
                            && t.UpdateAt > lastUpdateTime)
                .Select(t => new TagResponse(t.Id, t.Name, t.IsDeleted))
                .ToList();


            var articles =
                await dbContext.Articles.IgnoreQueryFilters().Include(a => a.Tags)
                    .Where(a => a.UserId == user.Id
                                && (a.CreateAt > lastUpdateTime
                                    || a.UpdateAt > lastUpdateTime)).ToListAsync(cancellationToken);

            var createdArticles = articles
                .Where(t => t.CreateAt > lastUpdateTime).Select(a =>
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
                        a.Tags.Select(a => a.Id).ToList(),
                        a.TimeToRead,
                        a.Content,
                        a.IsDeleted,
                        a.Location.ToString(),
                        a.Note);
                }).ToList();

            var updatedArticles = articles
                .Where(t => t.CreateAt <= lastUpdateTime
                            && t.UpdateAt != null
                            && t.UpdateAt > lastUpdateTime).Select(a =>
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
                        a.IsDeleted,
                        a.Location.ToString(),
                        a.Note);
                }).ToList();

            var highlights = await dbContext.Highlights.IgnoreQueryFilters()
                .Where(h => (h.CreateAt > lastUpdateTime
                             || h.UpdateAt > lastUpdateTime) && h.UserId == user.Id)
                .ToListAsync(cancellationToken);

            var createdHighlights = highlights
                .Where(h => h.CreateAt > lastUpdateTime)
                .Select(h =>
                {
                    var createAt = h.CreateAt.ToUnixTimeMilliseconds();
                    var updateAt = h.UpdateAt?.ToUnixTimeMilliseconds();
                    return new HighlightResponse(
                        h.Id,
                        h.ArticleId,
                        h.Location,
                        h.Html,
                        h.Markdown,
                        h.PlainText,
                        createAt,
                        updateAt,
                        h.Color,
                        h.IsDeleted,
                        h.CreateBy,
                        h.Note);
                })
                .ToList();

            var updatedHighlights = highlights
                .Where(h => h.CreateAt <= lastUpdateTime
                            && h.UpdateAt != null
                            && h.UpdateAt > lastUpdateTime)
                .Select(h =>
                {
                    var createAt = h.CreateAt.ToUnixTimeMilliseconds();
                    var updateAt = h.UpdateAt?.ToUnixTimeMilliseconds();
                    return new HighlightResponse(
                        h.Id,
                        h.ArticleId,
                        h.Location,
                        h.Html,
                        h.Markdown,
                        h.PlainText,
                        createAt,
                        updateAt,
                        h.Color,
                        h.IsDeleted,
                        h.CreateBy,
                        h.Note);
                })
                .ToList();

            var created = new Created(createdArticles, createdTags, createdHighlights);
            var updated = new Updated(updatedArticles, updatedTags, updatedHighlights);
            return new Response(created, updated);
        }
    }
}