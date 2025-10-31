using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Users;

namespace Rehi.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.FullName);

        builder.HasMany(u => u.UserSubscriptions)
            .WithOne(us => us.User)
            .HasForeignKey(us => us.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}