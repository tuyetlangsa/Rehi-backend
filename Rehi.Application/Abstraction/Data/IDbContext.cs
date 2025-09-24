using Microsoft.EntityFrameworkCore;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Abstraction.Data;

public interface IDbContext
{
    DbSet<Article> Articles { get; set; } 
    DbSet<User> Users { get; set; } 

    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<OutboxMessageConsumer> OutboxMessageConsumers { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}