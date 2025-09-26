using Rehi.Domain.Common;

namespace Rehi.Domain.Users;

public class User : Entity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
}