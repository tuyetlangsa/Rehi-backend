namespace Rehi.Application.Abstraction.Authentication;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string? passwordHash, string password);
}
