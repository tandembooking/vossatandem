using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace tandembooking.Migrations
{
    public partial class UserCleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_PaymentAccounts_VippsPaymentAccountIdIdId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_VippsPaymentAccountIdIdId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IZettleAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VippsPaymentAccountIdIdId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_VippsPaymentAccountId",
                table: "AspNetUsers",
                column: "VippsPaymentAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_PaymentAccounts_VippsPaymentAccountId",
                table: "AspNetUsers",
                column: "VippsPaymentAccountId",
                principalTable: "PaymentAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_PaymentAccounts_VippsPaymentAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_VippsPaymentAccountId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "IZettleAccountId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VippsPaymentAccountIdIdId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_VippsPaymentAccountIdIdId",
                table: "AspNetUsers",
                column: "VippsPaymentAccountIdIdId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_PaymentAccounts_VippsPaymentAccountIdIdId",
                table: "AspNetUsers",
                column: "VippsPaymentAccountIdIdId",
                principalTable: "PaymentAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
