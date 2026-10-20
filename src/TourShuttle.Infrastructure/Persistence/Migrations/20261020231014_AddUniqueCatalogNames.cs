using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourShuttle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCatalogNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PickupPoints_Name",
                table: "PickupPoints",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_Name",
                table: "Destinations",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PickupPoints_Name",
                table: "PickupPoints");

            migrationBuilder.DropIndex(
                name: "IX_Destinations_Name",
                table: "Destinations");
        }
    }
}
