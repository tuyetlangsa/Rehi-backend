using Rehi.Domain.Common;

namespace Rehi.Domain.Articles;

public class ArticleTag : Entity
{
    public Guid Id { get; set; }
    public Guid TagId { get; set; }
    public Guid ArticleId { get; set; }
}