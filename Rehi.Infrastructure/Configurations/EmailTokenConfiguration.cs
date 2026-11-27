using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Users;

namespace Rehi.Infrastructure.Configurations;

public class EmailTokenConfiguration : IEntityTypeConfiguration<EmailToken>
{
    public void Configure(EntityTypeBuilder<EmailToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
