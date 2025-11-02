using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Articles.CreateArticle;

public abstract class CreateArticle
{
    public record Command(Guid Id, string Url, string RawHtml, string Title, long CreateAt) : ICommand<Response>;

    public record Response(
        Guid Id,
        string Url,
        bool IsSavedBefore,
        List<HighlightResponse> Highlights
    );

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
        string CreateBy);

    internal class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail, cancellationToken);

            if (user is null) return Result.Failure<Response>(UserErrors.NotFound);

            var articleExisted = await dbContext.Articles.Include(a => a.Highlights)
                .SingleOrDefaultAsync(a => a.Url == command.Url, cancellationToken);

            if (articleExisted is not null)
            {
                var highlightResponses = articleExisted.Highlights.Select(h =>
                {
                    return new HighlightResponse(
                        h.Id,
                        h.ArticleId,
                        h.Location,
                        h.Html,
                        h.Markdown,
                        h.PlainText,
                        h.CreateAt.ToUnixTimeMilliseconds(),
                        h.UpdateAt?.ToUnixTimeMilliseconds(),
                        h.Color,
                        h.IsDeleted,
                        h.CreateBy);
                }).ToList();
                return new Response(
                    articleExisted.Id,
                    articleExisted.Url,
                    true,
                    highlightResponses);
            }

            var createAt = DateTimeOffset.FromUnixTimeMilliseconds(command.CreateAt);
            var article = new Article
            {
                Id = command.Id,
                Url = command.Url,
                RawHtml = command.RawHtml,
                UserId = user!.Id,
                CreateAt = createAt,
                Title = command.Title
            };

            article.Raise(new ArticleCreatedDomainEvent(article.Id));
            dbContext.Articles.Add(article);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new Response(article.Id, article.Url, false, []);
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Url).NotEmpty();
            RuleFor(x => x.RawHtml).NotEmpty();
            RuleFor(x => x.CreateAt).NotEmpty();
        }
    }
}