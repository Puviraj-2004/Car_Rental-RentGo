using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Car_Rental.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DamageReports_DamageReportId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "DamageReports");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DamageReportId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DamageReportId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DamageReportId",
                table: "Bookings");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraFee",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtraFeeReason",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BookingReference",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraFee",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExtraFeeReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BookingReference",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "DamageReportId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DamageReportId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DamageReports",
                columns: table => new
                {
                    DamageReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    InsuranceID = table.Column<int>(type: "int", nullable: true),
                    CarId = table.Column<int>(type: "int", nullable: true),
                    ClaimAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DamageImageUrls = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstimatedRepairCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ReportedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserPayAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamageReports", x => x.DamageReportId);
                    table.ForeignKey(
                        name: "FK_DamageReports_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DamageReports_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "CarId");
                    table.ForeignKey(
                        name: "FK_DamageReports_Insurances_InsuranceID",
                        column: x => x.InsuranceID,
                        principalTable: "Insurances",
                        principalColumn: "InsuranceID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DamageReportId",
                table: "Payments",
                column: "DamageReportId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_BookingId",
                table: "DamageReports",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_CarId",
                table: "DamageReports",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_InsuranceID",
                table: "DamageReports",
                column: "InsuranceID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DamageReports_DamageReportId",
                table: "Payments",
                column: "DamageReportId",
                principalTable: "DamageReports",
                principalColumn: "DamageReportId");
        }
    }
}
