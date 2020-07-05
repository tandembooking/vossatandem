using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace tandembooking.Migrations
{
    public partial class AddedPayments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IZettleAccount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VippsAccount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IZettleAccount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VippsAccount",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentAccountId",
                table: "Bookings",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IZettleAccountId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IZettlePaymentAccountId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentAdmin",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "VippsPaymentAccountId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VippsPaymentAccountIdIdId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ExternalRef = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAccount", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentAccountId",
                table: "Bookings",
                column: "PaymentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IZettlePaymentAccountId",
                table: "AspNetUsers",
                column: "IZettlePaymentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_VippsPaymentAccountIdIdId",
                table: "AspNetUsers",
                column: "VippsPaymentAccountIdIdId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_PaymentAccount_IZettlePaymentAccountId",
                table: "AspNetUsers",
                column: "IZettlePaymentAccountId",
                principalTable: "PaymentAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_PaymentAccount_VippsPaymentAccountIdIdId",
                table: "AspNetUsers",
                column: "VippsPaymentAccountIdIdId",
                principalTable: "PaymentAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_PaymentAccount_PaymentAccountId",
                table: "Bookings",
                column: "PaymentAccountId",
                principalTable: "PaymentAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_PaymentAccount_IZettlePaymentAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_PaymentAccount_VippsPaymentAccountIdIdId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_PaymentAccount_PaymentAccountId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "PaymentAccount");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentAccountId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IZettlePaymentAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_VippsPaymentAccountIdIdId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PaymentAccountId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IZettleAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IZettlePaymentAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsPaymentAdmin",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VippsPaymentAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VippsPaymentAccountIdIdId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "IZettleAccount",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VippsAccount",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IZettleAccount",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VippsAccount",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
