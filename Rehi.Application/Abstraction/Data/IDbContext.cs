using Microsoft.EntityFrameworkCore;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Highlights;
using Rehi.Domain.Tags;
using Rehi.Domain.Users;

namespace Rehi.Application.Abstraction.Data;

public interface IDbContext
{
    DbSet<Article> Articles { get; set; } 
    DbSet<User> Users { get; set; } 

    DbSet<Tag> Tags { get; set; }
    DbSet<ArticleTag> ArticleTags { get; set; }
    DbSet<Highlight> Highlights { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<OutboxMessageConsumer> OutboxMessageConsumers { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}