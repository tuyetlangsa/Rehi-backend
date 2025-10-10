using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Articles;
using Rehi.Domain.Tags;
using Rehi.Domain.Users;

namespace Rehi.Infrastructure.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.HasOne<User>().WithMany(u => u.Tags).HasForeignKey(t => t.UserId);
        builder.HasMany<Article>().WithMany(x => x.Tags).UsingEntity<ArticleTag>();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.Property(e => e.CreateAt).HasColumnType("timestamptz")
            .IsRequired();
        builder.Property(e => e.UpdateAt).HasColumnType("timestamptz").IsRequired(false);

    }
}