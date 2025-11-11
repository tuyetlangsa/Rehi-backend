using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class update_externalsubcriptionid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PayPalSubscriptionId",
                schema: "public",
                table: "UserSubscriptions",
                newName: "ExternalSubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExternalSubscriptionId",
                schema: "public",
                table: "UserSubscriptions",
                newName: "PayPalSubscriptionId");
        }
    }
}
