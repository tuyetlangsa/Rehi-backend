using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Domain.Highlights;

public class Highlight : Entity
{
    public Guid Id { get; set; }
    public string Location { get; set; } = null!;
    public string Html { get; set; } = null!;
    public string Markdown { get; set; } = null!;
    public string PlainText { get; set; } = null!;
    public Guid ArticleId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreateAt { get; set; }
    public DateTimeOffset? UpdateAt { get; set; }
    public string? Color { get; set; }
    public bool IsDeleted { get; set; }
    public string CreateBy { get; set; } = null!;
    public string? Note { get; set; }

    public virtual Article Article { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}