using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7AnalyticsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_expenses_Category",
                table: "expenses",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_Date",
                table: "expenses",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_MerchantName",
                table: "expenses",
                column: "MerchantName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_expenses_Category",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "IX_expenses_Date",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "IX_expenses_MerchantName",
                table: "expenses");
        }
    }
}
