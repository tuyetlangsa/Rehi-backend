using Microsoft.AspNetCore.Identity;
using Rehi.Domain.Users;
using IPasswordHasher = Rehi.Application.Abstraction.Authentication.IPasswordHasher;

namespace Rehi.Infrastructure.Authentication;

internal sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();
    private static readonly User Subject = new();
    private static readonly string UnusableHash = Hasher.HashPassword(Subject, Guid.NewGuid().ToString());

    public string Hash(string password) => Hasher.HashPassword(Subject, password);

    public bool Verify(string? passwordHash, string password)
    {
        var result = Hasher.VerifyHashedPassword(Subject, passwordHash ?? UnusableHash, password);
        return passwordHash is not null && result != PasswordVerificationResult.Failed;
    }
}
