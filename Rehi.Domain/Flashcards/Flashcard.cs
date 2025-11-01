using Rehi.Domain.Common;
using Rehi.Domain.Highlights;

namespace Rehi.Domain.Flashcards;

public class Flashcard : Entity
{
    public Guid Id { get; set; }
    public Guid HighlightId { get; set; }

    public FlashcardState State { get; set; }

    public int StepIndex { get; set; }
    public int Interval { get; set; }
    public double EaseFactor { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public Highlight Highlight { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public ICollection<FlashCardReview> Reviews { get; set; } = new List<FlashCardReview>();
}