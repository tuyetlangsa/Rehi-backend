namespace Rehi.Domain.Users;

public class EmailToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public EmailTokenPurpose Purpose { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public User User { get; set; } = null!;
}
