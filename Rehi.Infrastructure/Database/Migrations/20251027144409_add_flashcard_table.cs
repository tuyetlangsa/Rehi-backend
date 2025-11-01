using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class add_flashcard_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Flashcards",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HighlightId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<byte>(type: "smallint", nullable: false),
                    StepIndex = table.Column<int>(type: "integer", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    EaseFactor = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flashcards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flashcards_Highlights_HighlightId",
                        column: x => x.HighlightId,
                        principalSchema: "public",
                        principalTable: "Highlights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlashCardReviews",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlashcardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Feedback = table.Column<byte>(type: "smallint", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IntervalBefore = table.Column<int>(type: "integer", nullable: false),
                    EaseFactorBefore = table.Column<double>(type: "double precision", nullable: false),
                    IntervalAfter = table.Column<int>(type: "integer", nullable: false),
                    EaseFactorAfter = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashCardReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashCardReviews_Flashcards_FlashcardId",
                        column: x => x.FlashcardId,
                        principalSchema: "public",
                        principalTable: "Flashcards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlashCardReviews_FlashcardId",
                schema: "public",
                table: "FlashCardReviews",
                column: "FlashcardId");

            migrationBuilder.CreateIndex(
                name: "IX_Flashcards_HighlightId",
                schema: "public",
                table: "Flashcards",
                column: "HighlightId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlashCardReviews",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Flashcards",
                schema: "public");
        }
    }
}
