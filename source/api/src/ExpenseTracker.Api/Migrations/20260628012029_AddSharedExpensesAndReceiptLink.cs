using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedExpensesAndReceiptLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExpenseId",
                table: "receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "expenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "expense_shares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_expense_shares_expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_ExpenseId",
                table: "receipts",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_expense_shares_ExpenseId",
                table: "expense_shares",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_expense_shares_UserId",
                table: "expense_shares",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_shares");

            migrationBuilder.DropIndex(
                name: "IX_receipts_ExpenseId",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "ExpenseId",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "expenses");
        }
    }
}
