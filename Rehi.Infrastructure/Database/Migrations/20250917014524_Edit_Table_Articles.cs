using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Edit_Table_Articles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Author",
                schema: "public",
                table: "Articles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                schema: "public",
                table: "Articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "public",
                table: "Articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                schema: "public",
                table: "Articles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishDate",
                schema: "public",
                table: "Articles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaveUsing",
                schema: "public",
                table: "Articles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                schema: "public",
                table: "Articles",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextContent",
                schema: "public",
                table: "Articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeToRead",
                schema: "public",
                table: "Articles",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                schema: "public",
                table: "Articles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WordCount",
                schema: "public",
                table: "Articles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Author",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Content",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Language",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "PublishDate",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SaveUsing",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Summary",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "TextContent",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "TimeToRead",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Url",
                schema: "public",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "WordCount",
                schema: "public",
                table: "Articles");
        }
    }
}
