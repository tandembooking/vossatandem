using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace tandembooking.Migrations
{
    public partial class AddedPaymentAccountType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "PaymentAccount",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "PaymentAccount",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PaymentType = table.Column<int>(nullable: false),
                    ExternalRef = table.Column<string>(nullable: true),
                    Amount = table.Column<decimal>(nullable: false),
                    UnreconciledAmount = table.Column<decimal>(nullable: false),
                    InsertDate = table.Column<DateTime>(nullable: true),
                    ConfirmedDate = table.Column<DateTime>(nullable: true),
                    PaymentAccountId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_PaymentAccount_PaymentAccountId",
                        column: x => x.PaymentAccountId,
                        principalTable: "PaymentAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentAccountId",
                table: "Payments",
                column: "PaymentAccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "PaymentAccount");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "PaymentAccount");
        }
    }
}
