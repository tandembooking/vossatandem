using Microsoft.EntityFrameworkCore.Migrations;

namespace tandembooking.Migrations
{
    public partial class BookingPaymentAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IZettleAccount",
                table: "Bookings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VippsAccount",
                table: "Bookings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IZettleAccount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VippsAccount",
                table: "Bookings");
        }
    }
}
