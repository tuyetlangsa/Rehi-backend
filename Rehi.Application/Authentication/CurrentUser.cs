namespace Rehi.Application.Authentication;

public class CurrentUser
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required bool EmailVerified { get; set; }
}