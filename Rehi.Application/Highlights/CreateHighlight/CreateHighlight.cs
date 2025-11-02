using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Highlights;
using Rehi.Domain.Users;

namespace Rehi.Application.Highlights.CreateHighlight;

public abstract class CreateHighlight
{
    public record Command(
        Guid Id,
        string Location,
        string Html,
        string Markdown,
        string PlainText,
        Guid ArticleId,
        long CreateAt,
        string Color,
        string CreateBy)
        : ICommand<Guid>;

    internal sealed class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command command, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail, cancellationToken);

            if (user is null) return Result.Failure<Guid>(UserErrors.NotFound);

            var article = await dbContext.Articles
                .SingleOrDefaultAsync(a => a.Id == command.ArticleId, cancellationToken);

            if (article is null) return Result.Failure<Guid>(ArticleErrors.NotFound);
            var createAt = DateTimeOffset.FromUnixTimeMilliseconds(command.CreateAt);

            var highlight = new Highlight
            {
                Id = command.Id,
                Location = command.Location,
                Html = command.Html,
                Markdown = command.Markdown,
                PlainText = command.PlainText,
                ArticleId = command.ArticleId,
                CreateAt = createAt,
                Color = command.Color,
                UserId = user!.Id,
                CreateBy = command.CreateBy
            };

            highlight.Raise(new HighlightCreatedDomainEvent(highlight.Id));
            dbContext.Highlights.Add(highlight);
            await dbContext.SaveChangesAsync(cancellationToken);
            return highlight.Id;
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ArticleId)
                .NotEmpty().WithMessage("Highlight ID is required.");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required.");

            RuleFor(x => x.Html)
                .NotEmpty().WithMessage("HTML content is required.");

            RuleFor(x => x.Markdown)
                .NotEmpty().WithMessage("Markdown content is required.");

            RuleFor(x => x.PlainText)
                .NotEmpty().WithMessage("Plain text content is required.");

            RuleFor(x => x.ArticleId)
                .NotEmpty().WithMessage("Article ID is required.");

            RuleFor(x => x.CreateAt)
                .NotEmpty().WithMessage("Creation timestamp is required.");
            RuleFor(x => x.CreateBy)
                .NotEmpty().WithMessage("CreateBy is required.");
        }
    }
}