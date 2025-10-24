using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Articles;

namespace Rehi.Infrastructure.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(256);
        builder.Property(e => e.RawHtml).IsRequired().HasColumnType("text");
        builder.Property(e => e.Url).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.Author).HasMaxLength(128);
        builder.Property(e => e.Summary).HasMaxLength(512);
        builder.Property(e => e.ImageUrl).HasColumnType("text");
        builder.Property(e => e.Content).HasColumnType("text");
        builder.Property(e => e.TextContent).HasColumnType("text");
        builder.Property(e => e.Language).HasMaxLength(16);
        builder.Property(e => e.SaveUsing).HasMaxLength(64);
        builder.Property(e => e.WordCount);
        builder.Property(e => e.TimeToRead);
        builder.Property(e => e.PublishDate).HasColumnType("timestamptz");
        builder.HasOne(e => e.User).WithMany(u => u.Articles).HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.Property(e => e.CreateAt).HasColumnType("timestamptz")
            .IsRequired();
        builder.Property(e => e.UpdateAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(e => e.Location).HasConversion<byte>().IsRequired().HasDefaultValue(Location.Reading);
        builder.Property(e => e.Note).HasColumnType("text").IsRequired(false);
    }
}