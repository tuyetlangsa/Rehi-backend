using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Data;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth;

internal static class PendingEmailTokens
{
    public static async Task VoidAsync(IDbContext dbContext, Guid userId, EmailTokenPurpose purpose, DateTime now,
        CancellationToken cancellationToken)
    {
        var pending = await dbContext.EmailTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in pending) token.ConsumedAt = now;
    }
}
