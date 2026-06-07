using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AuditAndExpenseTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── audit_logs ────────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            // Index for Owner dashboard queries: filter by user, action, date.
            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId_CreatedAt",
                table: "audit_logs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Action",
                table: "audit_logs",
                column: "Action");

            // ── Append-only enforcement via PostgreSQL Row Level Security ─────────────
            //
            // All connections use the superuser 'postgres' role (from appsettings.json
            // ConnectionStrings:DefaultConnection) which bypasses RLS by default.
            // The RLS policy below targets the APPLICATION role 'expense_app' — a
            // restricted role that should be created for production deployments.
            //
            // For local development (superuser connection), RLS has no effect because
            // superusers always bypass RLS unless FORCE ROW LEVEL SECURITY is set.
            // That is intentional: the Owner GET /audit endpoint reads all rows via the
            // same superuser connection used by EF Core.
            //
            // To enforce RLS in production:
            //   1. CREATE ROLE expense_app WITH LOGIN PASSWORD '...';
            //   2. GRANT SELECT, INSERT ON audit_logs TO expense_app;
            //   3. Update ConnectionStrings:DefaultConnection to use expense_app.
            //   4. The SELECT policy below allows reads; no UPDATE/DELETE policy
            //      means those operations are denied for expense_app.
            migrationBuilder.Sql(@"
                ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;

                -- Allow INSERT for all (both superuser and expense_app).
                CREATE POLICY audit_insert_only ON audit_logs
                    FOR INSERT WITH CHECK (true);

                -- Allow SELECT for all (Owner reads via API; superuser reads unrestricted).
                CREATE POLICY audit_select_all ON audit_logs
                    FOR SELECT USING (true);

                -- No UPDATE or DELETE policies are created.
                -- For a non-superuser role this means UPDATE and DELETE are denied.
            ");

            // ── expenses ──────────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantName = table.Column<string>(type: "text", nullable: true),
                    MerchantAddress = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Time = table.Column<string>(type: "text", nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Total = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    OcrStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_expenses_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_ReceiptId",
                table: "expenses",
                column: "ReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expenses_UserId",
                table: "expenses",
                column: "UserId");

            // ── expense_items ─────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "expense_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_expense_items_expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expense_items_ExpenseId",
                table: "expense_items",
                column: "ExpenseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "expense_items");
            migrationBuilder.DropTable(name: "expenses");

            // Remove RLS policies before dropping the table.
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS audit_insert_only ON audit_logs;
                DROP POLICY IF EXISTS audit_select_all ON audit_logs;
            ");
            migrationBuilder.DropTable(name: "audit_logs");
        }
    }
}
