using Rehi.Domain.Users;

namespace Rehi.Application.Abstraction.Authentication;

public interface ITokenIssuer
{
    AccessToken IssueAccessToken(User user, Guid sessionId);
}

public sealed record AccessToken(string Value, DateTime ExpiresAt);
