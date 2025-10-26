using Rehi.Domain.Subscription;
using Rehi.Infrastructure.Database;

public class DbInitializer
{
public static void Seed(ApplicationDbContext context)
    {
        context.Database.EnsureCreated();
        // Seed Subscription Plans
        if (!context.SubscriptionPlans.Any())
        {
            var subscriptionPlans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan
                {
                    Name = "Freemium (Monthly)",
                    Price = 0,
                    Description = "Perfect for new users. Includes: Basic note taking, Save up to 10 articles per day, Manual flashcard creation, Unlimited access, Cross device access.",
                    DurationDays = 30,
                },
                new SubscriptionPlan
                {
                    Name = "Premium Individual (Monthly)",
                    Price = 2.9M,
                    Description = "Ideal for university students or researchers. Includes: Smart notes powered by AI, Auto flashcards, Quick note formatting, Spaced repetition reminders, Unlimited materials.",
                    DurationDays = 30,
                    PaypalPlanId = "P-4XP87117T0561114KND6LNKQ"
                },
                new SubscriptionPlan
                {
                    Name = "Group Plan (Monthly)",
                    Price = 10M,
                    Description = "Designed for study groups or teams. Includes: All Premium features, Shared group notes, Group flashcards, Personalized suggestions, Group dashboard.",
                    DurationDays = 30,
                    PaypalPlanId = "P-06H31651K2c745930END6LNYA"
                },

                // ===== YEARLY =====
                new SubscriptionPlan
                {
                    Name = "Premium Individual (Yearly)",
                    Price = 29.9M,
                    Description = "Ideal for university students or researchers. Includes: Smart notes powered by AI, Auto flashcards, Mind maps, Translation, PDF export, Unlimited materials.",
                    DurationDays = 365,
                    PaypalPlanId = "P-5XF55308K77570807ND6LOKI"
                },
                new SubscriptionPlan
                {
                    Name = "Business Plan (Yearly)",
                    Price = 21M,
                    Description = "Tailored for schools or organizations. Includes: LMS, Admin dashboard, Role-based access, Customization, Full onboarding & support.",
                    DurationDays = 365,
                    PaypalPlanId = "P-2HE25455557244027ND6LOSQ"
                }
            };

            context.SubscriptionPlans.AddRange(subscriptionPlans);
            context.SaveChanges();
        }
    }
}