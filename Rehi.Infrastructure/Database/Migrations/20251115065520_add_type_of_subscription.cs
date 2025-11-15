using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehi.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class add_type_of_subscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeOfSubscription",
                schema: "public",
                table: "SubscriptionPlans",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "public",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa2"),
                column: "TypeOfSubscription",
                value: "year");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa3"),
                column: "TypeOfSubscription",
                value: "year");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa4"),
                column: "TypeOfSubscription",
                value: "month");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa5"),
                column: "TypeOfSubscription",
                value: "month");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                column: "TypeOfSubscription",
                value: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeOfSubscription",
                schema: "public",
                table: "SubscriptionPlans");
        }
    }
}
