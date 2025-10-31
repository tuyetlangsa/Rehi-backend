using Rehi.Domain.Common;

namespace Rehi.Domain.Users;

public static class UserErrors
{
    public static Error NotFound => new("User.NotFound", "Article not found", ErrorType.NotFound);

    public static Error EmailAlreadyExists =>
        new("User.EmailAlreadyExists", "Email already exists", ErrorType.Conflict);

    public static Error NoActiveSubscription => new("User.NoActiveSubscription",
        "User does not have an active subscription", ErrorType.Conflict);
}