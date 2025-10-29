using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Users;

namespace Rehi.Infrastructure.Configurations;

public class UserSubcriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.HasKey(us => us.Id);

        builder.Property(us => us.UserId).IsRequired();
        builder.Property(us => us.SubscriptionPlanId).IsRequired();

        builder.Property(us => us.StartDate).IsRequired();
        builder.Property(us => us.EndDate).IsRequired();
        
        builder.HasOne(us => us.User)
            .WithMany(u => u.UserSubscriptions)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(us => us.SubscriptionPlan)
            .WithMany(s => s.UserSubscriptions) 
            .HasForeignKey(us => us.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict); 

        builder.Ignore(us => us.IsActive);
    }
}