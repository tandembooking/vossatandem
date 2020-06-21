using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace tandembooking.Migrations
{
    public partial class BookkeepingFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BoatDriverFee",
                table: "Bookings",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "Bookings",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "Bookings",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PilotFee",
                table: "Bookings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoatDriverFee",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PilotFee",
                table: "Bookings");
        }
    }
}
