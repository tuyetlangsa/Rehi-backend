using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Users;

namespace Rehi.Application.Users;

public abstract class CreateUser
{
    public record Command(string Email, string FullName) : ICommand<Guid>;
    internal sealed class Handler(IDbContext dbContext) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken: cancellationToken);
            if (user is not null)
            {
                return user.Id;
            }
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
    }
}