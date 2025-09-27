using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "outbox_message_consumers",
                schema: "public",
                columns: table => new
                {
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message_consumers", x => new { x.OutboxMessageId, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "jsonb", maxLength: 2000, nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    RawHtml = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Author = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PublishDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    TextContent = table.Column<string>(type: "text", nullable: true),
                    SaveUsing = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    TimeToRead = table.Column<TimeSpan>(type: "interval", nullable: true),
                    WordCount = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_UserId",
                schema: "public",
                table: "Articles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "outbox_message_consumers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "public");
        }
    }
}
