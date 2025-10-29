using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Flashcards;

namespace Rehi.Infrastructure.Configurations;

public class FlashcardReviewConfiguration : IEntityTypeConfiguration<FlashCardReview>
{
    public void Configure(EntityTypeBuilder<FlashCardReview> builder)
    {
        
        builder.HasKey(fr => fr.Id);
        builder.HasOne(fr => fr.Flashcard).WithMany(f => f.Reviews)
            .HasForeignKey(fr => fr.FlashcardId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(fr => fr.ReviewedAt).IsRequired();
        builder.Property(fr => fr.Feedback).HasConversion<byte>().IsRequired();
        builder.Property(fr => fr.IntervalAfter).IsRequired();
        builder.Property(fr => fr.EaseFactorAfter).IsRequired();
        builder.Property(fr => fr.IntervalBefore).IsRequired();
        builder.Property(fr => fr.EaseFactorBefore).IsRequired();
        builder.HasQueryFilter(f => !f.Flashcard.Highlight.IsDeleted);

    }
}