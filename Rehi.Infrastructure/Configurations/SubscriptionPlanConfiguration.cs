using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehi.Domain.Subscription;
using Rehi.Domain.Users;

namespace Rehi.Infrastructure.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Name)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(sp => sp.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();  
        
        builder.Property(sp => sp.Description)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("text");
        
        builder.Property(sp => sp.PaypalPlanId)
            .HasMaxLength(100)
            .HasColumnType("text");

        builder.HasMany<UserSubscription>()
            .WithOne(us => us.SubscriptionPlan)
            .HasForeignKey(us => us.SubscriptionPlanId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}