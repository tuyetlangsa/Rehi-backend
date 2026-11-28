using Microsoft.EntityFrameworkCore;
using MimeKit;
using Rehi.Application.Abstraction.Data;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth;

public static class Emails
{
    public static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();

    public static bool IsBareAddress(string email) =>
        !email.Any(char.IsWhiteSpace)
        && MailboxAddress.TryParse(email, out var mailbox)
        && string.IsNullOrEmpty(mailbox.Name)
        && mailbox.Address == email;

    public static Task<User?> FindUserAsync(IDbContext dbContext, string normalizedEmail,
        CancellationToken cancellationToken) =>
        dbContext.Users
            .Where(u => u.Email.ToLower() == normalizedEmail)
            .OrderByDescending(u => u.Email == normalizedEmail)
            .ThenBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
