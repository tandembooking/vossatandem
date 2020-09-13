using Microsoft.EntityFrameworkCore.Migrations;

namespace tandembooking.Migrations
{
    public partial class RenamedPaymentAccount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentAccount_PaymentAccountId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentAccount",
                table: "PaymentAccount");

            migrationBuilder.RenameTable(
                name: "PaymentAccount",
                newName: "PaymentAccounts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentAccounts",
                table: "PaymentAccounts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_PaymentAccounts_IZettlePaymentAccountId",
                table: "AspNetUsers",
                column: "IZettlePaymentAccountId",
                principalTable: "PaymentAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_PaymentAccounts_VippsPaymentAccountIdIdId",
                table: "AspNetUsers",
                column: "VippsPaymentAccountIdIdId",
                principalTable: "PaymentAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_PaymentAccounts_PaymentAccountId",
                table: "Bookings",
                column: "PaymentAccountId",
                principalTable: "PaymentAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentAccounts_PaymentAccountId",
                table: "Payments",
                column: "PaymentAccountId",
                principalTable: "PaymentAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_PaymentAccounts_IZettlePaymentAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_PaymentAccounts_VippsPaymentAccountIdIdId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_PaymentAccounts_PaymentAccountId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentAccounts_PaymentAccountId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentAccounts",
                table: "PaymentAccounts");

            migrationBuilder.RenameTable(
                name: "PaymentAccounts",
                newName: "PaymentAccount");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentAccount",
                table: "PaymentAccount",
                column: "Id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentAccount_PaymentAccountId",
                table: "Payments",
                column: "PaymentAccountId",
                principalTable: "PaymentAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
