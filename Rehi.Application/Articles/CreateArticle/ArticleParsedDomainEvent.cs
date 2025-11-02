using Rehi.Domain.Common;

namespace Rehi.Application.Articles.CreateArticle;

public class ArticleParsedDomainEvent(Guid articleId) : DomainEvent
{
    public Guid ArticleId { get; } = articleId;
}