using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Highlights;
using Rehi.Domain.Users;

namespace Rehi.Application.Highlights.DeleteHighlight;

public abstract class DeleteHighlight
{
    public record Command(Guid HighlightId, long UpdateAt) : ICommand;
    
    internal class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail , cancellationToken);

            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound);
            }
            
            var highlightExisted = await dbContext.Highlights.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => command.HighlightId == a.Id , cancellationToken);

            if (highlightExisted is null)
            {
                return  Result.Failure(HighlightErrors.NotFound);
            }
            
       
            highlightExisted.IsDeleted = true;
            var updateAt = DateTimeOffset.FromUnixTimeMilliseconds(command.UpdateAt);
            if (updateAt < highlightExisted.UpdateAt)
            {
                return Result.Failure(CommonErrors.StaleRequest);
            }
            highlightExisted.UpdateAt = updateAt;
            
            await dbContext.SaveChangesAsync(cancellationToken);
            return  Result.Success();
        }
    }
    
    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        { 
            RuleFor(x => x.HighlightId).NotEmpty();
            RuleFor(x => x.UpdateAt).NotNull();
        }
    }
}
