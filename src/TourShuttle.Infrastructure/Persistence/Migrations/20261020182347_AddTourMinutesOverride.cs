using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourShuttle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTourMinutesOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinutesBetweenPickupPoints",
                table: "Tours",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tours_MinutesBetweenPickupPoints",
                table: "Tours",
                sql: "\"MinutesBetweenPickupPoints\" IS NULL OR \"MinutesBetweenPickupPoints\" BETWEEN 1 AND 30");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tours_MinutesBetweenPickupPoints",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "MinutesBetweenPickupPoints",
                table: "Tours");
        }
    }
}
