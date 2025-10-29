using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Tags;

namespace Rehi.Application.Tags;

public abstract class CreateTag
{
    public record Command(string Name, long CreateAt) : ICommand<Guid>;

    internal sealed class Handler(IDbContext dbContext, IUserContext userContext) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var isExisted = 
                await dbContext.Tags
                    .SingleOrDefaultAsync(t => t.Name == request.Name, 
                        cancellationToken: cancellationToken);
            if (isExisted is not null)
            {
                return Result.Failure<Guid>(TagErrors.AlreadyExisted);
            }

            var user = await dbContext.Users
                .SingleOrDefaultAsync(u => u.Email == userContext.Email, 
                    cancellationToken);
            var createAt = DateTimeOffset.FromUnixTimeMilliseconds(request.CreateAt);
            var tag = new Tag()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                UserId = user!.Id,
                CreateAt =  createAt
            };

            dbContext.Tags.Add(tag);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success(tag.Id);
        }
    }
    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.CreateAt).NotEmpty();
        }
    }
}