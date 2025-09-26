using Rehi.Domain.Common;

namespace Rehi.Domain.Articles;

public class Article : Entity
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public string RawHtml { get; set; } = null!;
    public string? Title { get; set; } 
    public string? Author { get; set; }
    public string? Summary { get; set; }
    public DateTime? PublishDate { get; set; }
    public string? ImageUrl { get; set; }
    public string? Content { get; set; }
    public string? TextContent { get; set; }
    
    public string? SaveUsing { get; set; }
    public string? Language { get; set; }
    public TimeSpan? TimeToRead { get; set; }
    public int? WordCount { get; set; }
    public Guid UserId { get; set; }
}