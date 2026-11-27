using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.SendEmailToken;

public class EmailTokenRequestedDomainEvent(Guid userId, EmailTokenPurpose purpose) : DomainEvent
{
    public Guid UserId { get; } = userId;
    public EmailTokenPurpose Purpose { get; } = purpose;
}
