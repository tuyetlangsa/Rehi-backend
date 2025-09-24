namespace Rehi.Application.Abstraction.Authentication;

public class IUserContext
{
    Guid UserId { get; }
    string Email { get;  }
}