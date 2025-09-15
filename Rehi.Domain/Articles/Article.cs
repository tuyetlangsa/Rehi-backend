using Rehi.Domain.Common;

namespace Rehi.Domain.Articles;

public class Article : Entity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string RawHtml { get; set; } = null!;
}