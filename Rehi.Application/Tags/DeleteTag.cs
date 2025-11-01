using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Tags;

namespace Rehi.Application.Tags;

public abstract class DeleteTag
{
    public record Command(string Name, long DeleteAt) : ICommand;
    internal sealed class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var isExisted = 
                await dbContext.Tags
                    .SingleOrDefaultAsync(t => t.Name == request.Name, 
                        cancellationToken: cancellationToken);

            if (isExisted is null)
            {
                return Result.Failure(TagErrors.NotFound);
            }
            var deleteAt = DateTimeOffset.FromUnixTimeMilliseconds(request.DeleteAt);
            if (deleteAt < isExisted.UpdateAt)
            {
                return Result.Failure(CommonErrors.StaleRequest);
            }
            isExisted.IsDeleted = true;
            isExisted.UpdateAt = deleteAt;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.DeleteAt).NotEmpty();
        }
    }
}