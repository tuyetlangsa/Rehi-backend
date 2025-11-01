namespace Rehi.Domain.Flashcards;

public class FlashCardReview
{
    public Guid Id { get; set; }
    public Guid FlashcardId { get; set; }
    public ReviewFeedback Feedback { get; set; }
    public DateTime ReviewedAt { get; set; }
    public int IntervalBefore { get; set; }
    public double EaseFactorBefore { get; set; }

    public int IntervalAfter { get; set; }
    public double EaseFactorAfter { get; set; }
    public Flashcard Flashcard { get; set; } = null!;
}


public enum ReviewFeedback
{
    Again,
    Hard,
    Good,
    Easy
}