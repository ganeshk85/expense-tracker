using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expenses_Receipts_ReceiptId",
                table: "expenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Receipts",
                table: "Receipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InviteTokens",
                table: "InviteTokens");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Receipts",
                newName: "receipts");

            migrationBuilder.RenameTable(
                name: "InviteTokens",
                newName: "invite_tokens");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "users",
                newName: "IX_users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_InviteTokens_Token",
                table: "invite_tokens",
                newName: "IX_invite_tokens_Token");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_receipts",
                table: "receipts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_invite_tokens",
                table: "invite_tokens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_receipts_ReceiptId",
                table: "expenses",
                column: "ReceiptId",
                principalTable: "receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expenses_receipts_ReceiptId",
                table: "expenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_receipts",
                table: "receipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invite_tokens",
                table: "invite_tokens");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "receipts",
                newName: "Receipts");

            migrationBuilder.RenameTable(
                name: "invite_tokens",
                newName: "InviteTokens");

            migrationBuilder.RenameIndex(
                name: "IX_users_Username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_invite_tokens_Token",
                table: "InviteTokens",
                newName: "IX_InviteTokens_Token");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Receipts",
                table: "Receipts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InviteTokens",
                table: "InviteTokens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_Receipts_ReceiptId",
                table: "expenses",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
