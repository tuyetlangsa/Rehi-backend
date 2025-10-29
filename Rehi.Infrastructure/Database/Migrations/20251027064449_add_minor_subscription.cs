using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                schema: "public",
                table: "UserSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "public",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodEnd",
                schema: "public",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            
            migrationBuilder.AddColumn<string>(
                name: "PaymentProvider",
                schema: "public",
                table: "UserSubscriptions",
                type: "text",
                nullable: false,
                defaultValue: "");
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_Users_UserId",
                schema: "public",
                table: "UserSubscriptions");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_Users_UserId",
                schema: "public",
                table: "UserSubscriptions",
                column: "UserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_Users_UserId",
                schema: "public",
                table: "UserSubscriptions");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_Users_UserId",
                schema: "public",
                table: "UserSubscriptions",
                column: "UserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                schema: "public",
                table: "UserSubscriptions");
            migrationBuilder.DropColumn(
                name: "AutoRenew",
                schema: "public",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "public",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodEnd",
                schema: "public",
                table: "UserSubscriptions");
        }
        
        
    }
}
