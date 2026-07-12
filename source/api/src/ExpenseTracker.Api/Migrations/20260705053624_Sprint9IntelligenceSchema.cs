using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint9IntelligenceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "merchant_aliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    AliasNormalized = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CanonicalNormalized = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_aliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "merchant_field_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantNameNormalized = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RegionX = table.Column<double>(type: "double precision", nullable: false),
                    RegionY = table.Column<double>(type: "double precision", nullable: false),
                    RegionW = table.Column<double>(type: "double precision", nullable: false),
                    RegionH = table.Column<double>(type: "double precision", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_field_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recurring_expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantNameNormalized = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AverageAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TypicalDayOfMonth = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastDetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    SnoozedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_expenses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_merchant_aliases_HouseholdId_AliasNormalized",
                table: "merchant_aliases",
                columns: new[] { "HouseholdId", "AliasNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_merchant_field_templates_HouseholdId_MerchantNameNormalized~",
                table: "merchant_field_templates",
                columns: new[] { "HouseholdId", "MerchantNameNormalized", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_expenses_HouseholdId_MerchantNameNormalized",
                table: "recurring_expenses",
                columns: new[] { "HouseholdId", "MerchantNameNormalized" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "merchant_aliases");

            migrationBuilder.DropTable(
                name: "merchant_field_templates");

            migrationBuilder.DropTable(
                name: "recurring_expenses");
        }
    }
}
