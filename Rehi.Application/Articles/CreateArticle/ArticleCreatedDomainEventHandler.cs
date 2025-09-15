using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;

namespace Rehi.Application.Articles.CreateArticle;

public class ArticleCreatedDomainEventHandler : DomainEventHandler<ArticleCreatedDomainEvent>
{
    public async override Task Handle(ArticleCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("AHIHIIH");
    }
}