using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class add_highlight_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Highlights",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Html = table.Column<string>(type: "text", nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    PlainText = table.Column<string>(type: "text", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdateAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Highlights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Highlights_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "public",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Highlights_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_ArticleId",
                schema: "public",
                table: "Highlights",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_UserId",
                schema: "public",
                table: "Highlights",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Highlights",
                schema: "public");
        }
    }
}
