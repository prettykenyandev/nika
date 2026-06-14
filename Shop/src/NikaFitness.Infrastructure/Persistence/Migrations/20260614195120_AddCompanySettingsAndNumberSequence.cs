using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NikaFitness.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettingsAndNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TradingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TaxIdentifier = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    default_tax_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    InvoiceNumberPrefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BillNumberPrefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PurchaseOrderNumberPrefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    InvoiceFooter = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PaymentInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "number_sequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    NextValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_number_sequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_number_sequences_Key",
                table: "number_sequences",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_settings");

            migrationBuilder.DropTable(
                name: "number_sequences");
        }
    }
}
