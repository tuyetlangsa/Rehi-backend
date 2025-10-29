using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Tags;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using Rehi.Domain.Tags;
using Rehi.Domain.Users;

namespace Rehi.Application.Articles.AssignArticleTag;


public abstract class AssignArticleTag
{
    public record Command(Guid ArticleId, string TagName, long CreateAt): ICommand;

    internal class Handler(IDbContext dbContext, IUserContext userContext, ISender sender) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail , cancellationToken);

            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound);
            }

            var article = await dbContext.Articles.Include(a => a.Tags)
                .SingleOrDefaultAsync(a => a.Id == command.ArticleId, cancellationToken);
            if (article is null)
            {
                return Result.Failure(ArticleErrors.NotFound);
            }
            
            var tag = await dbContext.Tags
                .SingleOrDefaultAsync(t => t.Name == command.TagName && t.UserId == user.Id, cancellationToken);
            if (tag is null)
            {
                var result = await sender.Send(new CreateTag.Command(command.TagName, command.CreateAt), cancellationToken);
                if (result.IsFailure)
                {
                    return Result.Failure(result.Error);
                }
                
                var tagId = result.Value;
                tag = await dbContext.Tags
                    .SingleOrDefaultAsync(t => t.Id == tagId, cancellationToken);   
                article.Tags.Add(tag!);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }

            if (article.Tags.Contains(tag))
            {
                return Result.Failure(TagErrors.TagAlreadyAssigned);
            }
            
            var updateAt = DateTimeOffset.FromUnixTimeMilliseconds(command.CreateAt);
            article.Tags.Add(tag);
            article.UpdateAt = updateAt;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
    
    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        { 
            RuleFor(x => x.ArticleId).NotEmpty();
            RuleFor(x => x.TagName).NotEmpty();
            RuleFor(x => x.CreateAt).NotEmpty();
        }
    }
}