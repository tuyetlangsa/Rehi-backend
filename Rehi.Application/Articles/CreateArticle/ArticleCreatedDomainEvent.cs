using Rehi.Domain.Common;

namespace Rehi.Application.Articles.CreateArticle;

public class ArticleCreatedDomainEvent(Guid articleId) : DomainEvent
{
    public Guid ArticleId { get; } = articleId;
}