using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class addnotefiledinarticletable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "public",
                table: "Articles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                schema: "public",
                table: "Articles");
        }
    }
}
