using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace tandembooking.Migrations
{
    public partial class VippsSettlement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaymentDate",
                table: "Payments",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "VippsSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PaymentAccountId = table.Column<Guid>(nullable: true),
                    ExternalRef = table.Column<string>(nullable: true),
                    ImportDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VippsSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VippsSettlements_PaymentAccounts_PaymentAccountId",
                        column: x => x.PaymentAccountId,
                        principalTable: "PaymentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VippsSettlements_PaymentAccountId",
                table: "VippsSettlements",
                column: "PaymentAccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VippsSettlements");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaymentDate",
                table: "Payments",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset));
        }
    }
}
