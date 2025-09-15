using FluentValidation;
using Rehi.Application.Abstraction.CurrentUser;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;

namespace Rehi.Application.Articles.CreateArticle;

public abstract class CreateArticle
{
    public record Command(string Title, string RawHtml): ICommand<Guid>;

    
    internal class Handler(IDbContext dbContext) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command command, CancellationToken cancellationToken)
        {
            var article = new Article()
            {
                Id = Guid.NewGuid(),
                Title = command.Title,
                RawHtml = command.RawHtml
            };
            
            article.Raise(new ArticleCreatedDomainEvent());
            dbContext.Articles.Add(article);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success(article.Id);
        }
    }
    
    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
           RuleFor(x => x.Title).NotEmpty();
           RuleFor(x => x.RawHtml).NotEmpty();
        }
    }
}