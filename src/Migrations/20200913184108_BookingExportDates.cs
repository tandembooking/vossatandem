using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace tandembooking.Migrations
{
    public partial class BookingExportDates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedDate",
                table: "Bookings",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledDate",
                table: "Bookings",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingPayment",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Amount = table.Column<decimal>(nullable: false),
                    InsertDate = table.Column<DateTime>(nullable: false),
                    PaymentId = table.Column<Guid>(nullable: false),
                    BookingId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingPayment_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingPayment_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayment_BookingId",
                table: "BookingPayment",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayment_PaymentId",
                table: "BookingPayment",
                column: "PaymentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingPayment");

            migrationBuilder.DropColumn(
                name: "ExportedDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReconciledDate",
                table: "Bookings");
        }
    }
}
