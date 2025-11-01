using Rehi.Application.Abstraction.Data;
using Rehi.Domain.Common;
using Rehi.Domain.Flashcards;

namespace Rehi.Application.Highlights.CreateHighlight;

public class HighlightCreatedDomainEventHandler(IDbContext dbContext) 
    : DomainEventHandler<HighlightCreatedDomainEvent>
{
    public override async Task Handle(HighlightCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var flashcard = new Flashcard()
        {
            Id = Guid.NewGuid(),
            HighlightId = domainEvent.HighlightId,
            State = FlashcardState.New,      
            StepIndex = 0,
            Interval = 0,
            EaseFactor = SpaceRepititionOptions.EaseFactorInitial,
            CreatedAt = DateTime.UtcNow,
            DueDate = null,
            LastReviewedAt = null
        };

        await dbContext.Flashcards.AddAsync(flashcard, cancellationToken);        
    }
}