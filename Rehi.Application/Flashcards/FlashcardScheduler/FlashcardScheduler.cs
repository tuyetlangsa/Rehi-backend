using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Flashcards;

namespace Rehi.Application.Flashcards.FlashcardScheduler;

public abstract class FlashcardScheduler
{
    public sealed record Command(
        Guid FlashcardId,
        ReviewFeedback Feedback,
        long ReviewedAt
    ) : ICommand;

    internal sealed class Handler(IDbContext dbContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var card = await dbContext.Flashcards
                .SingleOrDefaultAsync(c => c.Id == request.FlashcardId, cancellationToken);

            if (card is null)
            {
                return Result.Failure(FlashcardErrors.NotFound);
            }

            var reviewedAt = DateTimeOffset.FromUnixTimeMilliseconds(request.ReviewedAt).UtcDateTime;
            if (reviewedAt < card.LastReviewedAt)
            {
                return Result.Failure(new
                    Error("FlashcardScheduler.InvalidReviewedAt",
                        "ReviewedAt cannot be earlier than the last reviewed time.",
                        ErrorType.Conflict));
            }
            var review = new FlashCardReview()
            {
                FlashcardId = card.Id,
                Feedback = request.Feedback,
                ReviewedAt = reviewedAt,
                IntervalBefore = card.Interval,
                EaseFactorBefore = card.EaseFactor,
            };

            switch (card.State)
            {
                case FlashcardState.New:
                    card.State = FlashcardState.Learning;
                    card.StepIndex = 0;
                    card.DueDate = reviewedAt + SpaceRepititionOptions.LearningSteps[0];
                    break;

                case FlashcardState.Learning:
                    HandleLearning(card, request.Feedback, reviewedAt);
                    break;

                case FlashcardState.Review:
                    HandleReview(card, request.Feedback, reviewedAt);
                    break;
            }

            card.LastReviewedAt = reviewedAt;
            review.IntervalAfter = card.Interval;
            review.EaseFactorAfter = card.EaseFactor;
            dbContext.FlashCardReviews.Add(review);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        private void HandleLearning(Flashcard card, ReviewFeedback feedback, DateTime now)
        {
            if (feedback == ReviewFeedback.Again)
            {
                card.StepIndex = 0;
                card.DueDate = now + SpaceRepititionOptions.LearningSteps[0];
            }
            else if (feedback == ReviewFeedback.Hard)
            {
                card.DueDate = now + TimeSpan.FromTicks(
                    (long)((SpaceRepititionOptions.LearningSteps[card.StepIndex].Ticks +
                            SpaceRepititionOptions.LearningSteps.Last().Ticks) / 2)
                );
            }
            else if (feedback == ReviewFeedback.Good)
            {
                card.StepIndex++;
                if (card.StepIndex >= SpaceRepititionOptions.LearningSteps.Count)
                {
                    Graduate(card, now);
                }
                else
                {
                    card.DueDate = now + SpaceRepititionOptions.LearningSteps[card.StepIndex];
                }
            }
            else if (feedback == ReviewFeedback.Easy)
            {
                Graduate(card, now);
            }
        }

        private void HandleReview(Flashcard card, ReviewFeedback feedback, DateTime now)
        {
            switch (feedback)
            {
                case ReviewFeedback.Again:
                    card.State = FlashcardState.Learning;
                    card.StepIndex = 0;
                    card.DueDate = now + SpaceRepititionOptions.LearningSteps[0];
                    break;

                case ReviewFeedback.Hard:
                    card.EaseFactor = Math.Max(SpaceRepititionOptions.EaseFactorMin, card.EaseFactor - 0.15);
                    card.Interval = (int)(card.Interval * SpaceRepititionOptions.HardFactor);
                    card.DueDate = now.AddDays(card.Interval);
                    break;

                case ReviewFeedback.Good:
                    card.Interval = (int)(card.Interval * card.EaseFactor);
                    card.DueDate = now.AddDays(card.Interval);
                    break;

                case ReviewFeedback.Easy:
                    card.EaseFactor += 0.15;
                    card.Interval = (int)(card.Interval * card.EaseFactor * SpaceRepititionOptions.EasyBonus);
                    card.DueDate = now.AddDays(card.Interval);
                    break;
            }
        }

        private void Graduate(Flashcard card, DateTime now)
        {
            card.State = FlashcardState.Review;
            card.StepIndex = 0;
            card.Interval = SpaceRepititionOptions.GraduatingIntervalDays;
            card.DueDate = now.AddDays(card.Interval);
        }
    }
}