using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint8IntelligenceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "merchant_category_map",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantNameNormalized = table.Column<string>(
                        type: "character varying(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(
                        type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfirmedCount = table.Column<int>(type: "integer", nullable: false),
                    LastConfirmedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_category_map", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_dismissals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DismissedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DismissedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplicate_dismissals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "merchant_tag_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantNameNormalized = table.Column<string>(
                        type: "character varying(512)", maxLength: 512, nullable: false),
                    Tag = table.Column<string>(
                        type: "character varying(128)", maxLength: 128, nullable: false),
                    UseCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_tag_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ocr_field_accuracy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantNameNormalized = table.Column<string>(
                        type: "character varying(512)", maxLength: 512, nullable: false),
                    FieldName = table.Column<string>(
                        type: "character varying(64)", maxLength: 64, nullable: false),
                    TotalExtractions = table.Column<int>(type: "integer", nullable: false),
                    TotalCorrections = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocr_field_accuracy", x => x.Id);
                });

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_merchant_category_map_HouseholdId_MerchantNameNormalized",
                table: "merchant_category_map",
                columns: new[] { "HouseholdId", "MerchantNameNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_duplicate_dismissals_ExpenseId",
                table: "duplicate_dismissals",
                column: "ExpenseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_merchant_tag_history_HouseholdId_MerchantNameNormalized_Tag",
                table: "merchant_tag_history",
                columns: new[] { "HouseholdId", "MerchantNameNormalized", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocr_field_accuracy_MerchantNameNormalized_FieldName",
                table: "ocr_field_accuracy",
                columns: new[] { "MerchantNameNormalized", "FieldName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "merchant_category_map");
            migrationBuilder.DropTable(name: "duplicate_dismissals");
            migrationBuilder.DropTable(name: "merchant_tag_history");
            migrationBuilder.DropTable(name: "ocr_field_accuracy");
        }
    }
}
