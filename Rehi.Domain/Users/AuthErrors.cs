using Rehi.Domain.Common;

namespace Rehi.Domain.Users;

public static class AuthErrors
{
    public static Error InvalidCredentials =>
        new("Auth.InvalidCredentials", "Email or password is incorrect", ErrorType.UnAuthorized);

    public static Error EmailNotVerified =>
        new("Auth.EmailNotVerified", "Verify your email before signing in", ErrorType.Forbidden);

    public static Error InvalidRefreshToken =>
        new("Auth.InvalidRefreshToken", "The refresh token is invalid or expired", ErrorType.UnAuthorized);

    public static Error SessionEnded =>
        new("Auth.SessionEnded", "This session has ended. Sign in again", ErrorType.UnAuthorized);

    public static Error InvalidEmailToken =>
        new("Auth.InvalidEmailToken", "The link is invalid or expired", ErrorType.Validation);
}
