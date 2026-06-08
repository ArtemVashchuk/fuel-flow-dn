using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelFlow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fuel_vouchers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fuel_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    liters = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    voucher_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    qr_payload = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuel_vouchers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "voucher_imports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    page_count = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    imported_count = table.Column<int>(type: "integer", nullable: false),
                    duplicate_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_imports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "voucher_import_errors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    voucher_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: false),
                    raw_text = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_import_errors", x => x.id);
                    table.ForeignKey(
                         name: "FK_voucher_import_errors_voucher_imports_import_id",
                         column: x => x.import_id,
                         principalTable: "voucher_imports",
                         principalColumn: "id",
                         onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fuel_vouchers_expiration_date",
                table: "fuel_vouchers",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_vouchers_fuel_type",
                table: "fuel_vouchers",
                column: "fuel_type");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_vouchers_provider",
                table: "fuel_vouchers",
                column: "provider");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_vouchers_qr_payload",
                table: "fuel_vouchers",
                column: "qr_payload",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fuel_vouchers_voucher_number",
                table: "fuel_vouchers",
                column: "voucher_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voucher_import_errors_import_id",
                table: "voucher_import_errors",
                column: "import_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fuel_vouchers");

            migrationBuilder.DropTable(
                name: "voucher_import_errors");

            migrationBuilder.DropTable(
                name: "voucher_imports");
        }
    }
}
