using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;
using Rehi.Domain.Common;
using Rehi.Domain.Highlights;
using Rehi.Domain.Tags;
using Rehi.Domain.Users;

namespace Rehi.Domain.Articles;

public class Article : Entity
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public string RawHtml { get; set; } = null!;
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset? PublishDate { get; set; }
    public string? ImageUrl { get; set; }
    public string? Content { get; set; }
    public string? TextContent { get; set; }

    public string? SaveUsing { get; set; }
    public string? Language { get; set; }
    public TimeSpan? TimeToRead { get; set; }
    public int? WordCount { get; set; }
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreateAt { get; set; }
    public DateTimeOffset? UpdateAt { get; set; }

    public Location Location { get; set; }
    public string? Note { get; set; }
    public virtual ICollection<Highlight> Highlights { get; set; } = new List<Highlight>();
}