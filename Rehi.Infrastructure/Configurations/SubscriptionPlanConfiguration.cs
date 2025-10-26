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

        builder.HasData(new List<SubscriptionPlan>
        {
            new SubscriptionPlan
            {
                Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                Name = "Freemium (Monthly)",
                Price = 0,
                Description =
                    "Perfect for new users. Includes: Basic note taking, Save up to 10 articles per day, Manual flashcard creation, Unlimited access, Cross device access.",
                DurationDays = 30,
            },
            new SubscriptionPlan
            {
                Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa5"),
                Name = "Premium Individual (Monthly)",
                Price = 2.9M,
                Description =
                    "Ideal for university students or researchers. Includes: Smart notes powered by AI, Auto flashcards, Quick note formatting, Spaced repetition reminders, Unlimited materials.",
                DurationDays = 30,
                PaypalPlanId = "P-4XP87117T0561114KND6LNKQ"
            },
            new SubscriptionPlan
            {
                Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa4"),
                Name = "Group Plan (Monthly)",
                Price = 10M,
                Description =
                    "Designed for study groups or teams. Includes: All Premium features, Shared group notes, Group flashcards, Personalized suggestions, Group dashboard.",
                DurationDays = 30,
                PaypalPlanId = "P-06H31651K2C745930END6LNYA"
            },
            new SubscriptionPlan
            {
                Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa3"),
                Name = "Premium Individual (Yearly)",
                Price = 29.9M,
                Description =
                    "Ideal for university students or researchers. Includes: Smart notes powered by AI, Auto flashcards, Mind maps, Translation, PDF export, Unlimited materials.",
                DurationDays = 365,
                PaypalPlanId = "P-5XF55308K77570807ND6LOKI"
            },
            new SubscriptionPlan
            {
                Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa2"),
                Name = "Business Plan (Yearly)",
                Price = 21M,
                Description =
                    "Tailored for schools or organizations. Includes: LMS, Admin dashboard, Role-based access, Customization, Full onboarding & support.",
                DurationDays = 365,
                PaypalPlanId = "P-2HE25455557244027ND6LOSQ"
            }
        });
    }
}