using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class add_subscription_plan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", maxLength: 500, nullable: false),
                    PaypalPlanId = table.Column<string>(type: "text", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PayPalSubscriptionId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalSchema: "public",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Description", "DurationDays", "Name", "PaypalPlanId", "Price" },
                values: new object[,]
                {
                    { new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa2"), "Tailored for schools or organizations. Includes: LMS, Admin dashboard, Role-based access, Customization, Full onboarding & support.", 365, "Business Plan (Yearly)", "P-2HE25455557244027ND6LOSQ", 21m },
                    { new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa3"), "Ideal for university students or researchers. Includes: Smart notes powered by AI, Auto flashcards, Mind maps, Translation, PDF export, Unlimited materials.", 365, "Premium Individual (Yearly)", "P-5XF55308K77570807ND6LOKI", 29.9m },
                    { new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa4"), "Designed for study groups or teams. Includes: All Premium features, Shared group notes, Group flashcards, Personalized suggestions, Group dashboard.", 30, "Group Plan (Monthly)", "P-06H31651K2C745930END6LNYA", 10m },
                    { new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa5"), "Ideal for university students or researchers. Includes: Smart notes powered by AI, Auto flashcards, Quick note formatting, Spaced repetition reminders, Unlimited materials.", 30, "Premium Individual (Monthly)", "P-4XP87117T0561114KND6LNKQ", 2.9m },
                    { new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6"), "Perfect for new users. Includes: Basic note taking, Save up to 10 articles per day, Manual flashcard creation, Unlimited access, Cross device access.", 30, "Freemium (Monthly)", null, 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                schema: "public",
                table: "UserSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                schema: "public",
                table: "UserSubscriptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSubscriptions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans",
                schema: "public");
        }
    }
}
