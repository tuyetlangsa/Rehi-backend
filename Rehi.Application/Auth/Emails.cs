namespace Rehi.Application.Auth;

public static class Emails
{
    public static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();
}
