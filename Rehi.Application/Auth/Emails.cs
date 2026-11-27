using MimeKit;

namespace Rehi.Application.Auth;

public static class Emails
{
    public static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();

    public static bool IsBareAddress(string email) =>
        !email.Any(char.IsWhiteSpace)
        && MailboxAddress.TryParse(email, out var mailbox)
        && string.IsNullOrEmpty(mailbox.Name)
        && mailbox.Address == email;
}
