using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class huhu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalSubscriptionId",
                schema: "public",
                table: "UserSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalSubscriptionId",
                schema: "public",
                table: "UserSubscriptions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
