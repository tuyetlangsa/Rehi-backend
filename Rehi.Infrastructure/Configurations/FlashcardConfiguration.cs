using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Flashcards;

namespace Rehi.Infrastructure.Configurations;

public class FlashcardConfiguration : IEntityTypeConfiguration<Flashcard>
{
    public void Configure(EntityTypeBuilder<Flashcard> builder)
    {
        builder.HasKey(f => f.Id);
        builder.HasOne(f => f.Highlight).WithOne()
            .HasForeignKey<Flashcard>(f => f.HighlightId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(f => f.State)
            .HasConversion<byte>()
            .IsRequired();
        
        builder.Property(f => f.StepIndex).IsRequired();
        builder.Property(f => f.Interval).IsRequired();
        builder.Property(f => f.EaseFactor).IsRequired();
        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.DueDate);
        builder.Property(f => f.LastReviewedAt);
        builder.Property(f => f.IsDeleted).IsRequired();
        builder.HasQueryFilter(f => !f.Highlight.IsDeleted);
    }
}