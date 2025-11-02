using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Highlights;

namespace Rehi.Infrastructure.Configurations;

public class HighlightConfiguration : IEntityTypeConfiguration<Highlight>
{
    public void Configure(EntityTypeBuilder<Highlight> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Html)
            .IsRequired()
            .HasColumnType("text");
        builder.Property(e => e.PlainText)
            .IsRequired()
            .HasColumnType("text");
        builder.Property(e => e.Markdown)
            .IsRequired()
            .HasColumnType("text");
        builder.Property(e => e.Color)
            .IsRequired(false)
            .HasMaxLength(32);
        builder.HasOne(e => e.User)
            .WithMany(u => u.Highlights)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Article)
            .WithMany(a => a.Highlights)
            .HasForeignKey(e => e.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false);
        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.Property(e => e.CreateAt)
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.Property(e => e.UpdateAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);
        builder.Property(e => e.CreateBy).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Note).HasColumnType("text").IsRequired(false);
    }
}