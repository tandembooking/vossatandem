using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace tandembooking.Migrations
{
    public partial class booking_location : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "PilotAvailabilities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingLocationId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PilotAvailabilities_LocationId",
                table: "PilotAvailabilities",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingLocationId",
                table: "Bookings",
                column: "BookingLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Location_BookingLocationId",
                table: "Bookings",
                column: "BookingLocationId",
                principalTable: "Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PilotAvailabilities_Location_LocationId",
                table: "PilotAvailabilities",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Location_BookingLocationId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_PilotAvailabilities_Location_LocationId",
                table: "PilotAvailabilities");

            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropIndex(
                name: "IX_PilotAvailabilities_LocationId",
                table: "PilotAvailabilities");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingLocationId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "PilotAvailabilities");

            migrationBuilder.DropColumn(
                name: "BookingLocationId",
                table: "Bookings");
        }
    }
}
