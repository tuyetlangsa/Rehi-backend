using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Tags;

namespace Rehi.Domain.Users;

public class User : Entity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public ICollection<Article> Articles { get; set; } = new List<Article>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public DateTime ScheduleTime { get; set; }

}