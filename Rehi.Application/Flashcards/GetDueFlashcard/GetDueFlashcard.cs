using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Flashcards;
using Rehi.Domain.Users;

namespace Rehi.Application.Flashcards.GetDueFlashcard;

public abstract class GetDueFlashcard
{
    public record Query : IQuery<List<FlashCardResponse>>;

    public record FlashCardResponse(
        Guid Id,
        Guid HighlightId,
        DateTime? DueDate,
        FlashcardState State
    );

    internal sealed class Handler(IDbContext dbContext, IUserContext userContext) : IQueryHandler<Query, List<FlashCardResponse>>
    {
        public async Task<Result<List<FlashCardResponse>>> Handle(Query query, CancellationToken cancellationToken)
        {
            // Implementation to get due flashcards goes here
            var now = DateTime.UtcNow;

            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == userEmail , cancellationToken);

            if (user is null)
            {
                return Result.Failure<List<FlashCardResponse>>(UserErrors.NotFound);
            }

            var highlightIds = await dbContext.Highlights
                .Where(h => h.UserId == user.Id)
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);
            
            var flashcards = await dbContext.Flashcards
                .Where(f => 
                    highlightIds.Contains(f.HighlightId) &&
                    (
                        f.State == FlashcardState.New ||
                        (f.DueDate != null && f.DueDate <= now)
                    )
                )
                .OrderBy(f => f.DueDate ?? DateTime.MinValue)
                .Select(f => new FlashCardResponse(
                    f.Id,
                    f.HighlightId,
                    f.DueDate,
                    f.State
                ))
                .ToListAsync(cancellationToken);
            
            return flashcards;
        }
    }
}