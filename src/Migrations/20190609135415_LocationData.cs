using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TandemBooking.Models;

namespace tandembooking.Migrations
{
    public partial class LocationData: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("Location", new string[]
            {
                nameof(Location.Id),
                nameof(Location.Name),
                nameof(Location.Active)
            }, new object[,]
            {
                {
                    Guid.NewGuid(),
                    "Voss",
                    true
                },
                {
                    Guid.NewGuid(),
                    "Myrkdalen",
                    true
                },
                {
                    Guid.NewGuid(),
                    "Aurland",
                    true
                }
            });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
