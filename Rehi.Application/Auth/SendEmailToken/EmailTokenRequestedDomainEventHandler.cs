using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rehi.Application.Abstraction.Clock;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Email;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth.SendEmailToken;

public class EmailTokenRequestedDomainEventHandler(
    IDbContext dbContext,
    ISendEmailService sendEmailService,
    IDateTimeProvider clock,
    IOptions<WebAppOptions> webAppOptions) : DomainEventHandler<EmailTokenRequestedDomainEvent>
{
    public override async Task Handle(EmailTokenRequestedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == domainEvent.UserId, cancellationToken);
        if (user is null) return;

        var mail = EmailTokenMail.For(domainEvent.Purpose);
        var now = clock.UtcNow;

        await PendingEmailTokens.VoidAsync(dbContext, user.Id, domainEvent.Purpose, now, cancellationToken);

        var emailToken = OpaqueToken.Create();
        dbContext.EmailTokens.Add(new EmailToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Purpose = domainEvent.Purpose,
            TokenHash = emailToken.Hash,
            CreatedAt = now,
            ExpiresAt = now + mail.Lifetime
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var link = $"{webAppOptions.Value.WebBaseUrl.TrimEnd('/')}{mail.Path}?token={emailToken.Value}";
        await sendEmailService.SendEmailAsync(user.Email, mail.Body(link), mail.Subject);
    }
}

internal sealed record EmailTokenMail(string Path, TimeSpan Lifetime, string Subject, Func<string, string> Body)
{
    public static EmailTokenMail For(EmailTokenPurpose purpose) => purpose switch
    {
        EmailTokenPurpose.VerifyEmail => new EmailTokenMail(
            "/verify-email",
            TimeSpan.FromHours(24),
            "Verify your Rehi email",
            link => $"<p>Confirm your email address to start using Rehi.</p><p><a href=\"{link}\">{link}</a></p><p>The link expires in 24 hours.</p>"),
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
    };
}
