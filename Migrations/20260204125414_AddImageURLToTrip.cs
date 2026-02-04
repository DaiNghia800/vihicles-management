using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Public_Transport.Migrations
{
    /// <inheritdoc />
    public partial class AddImageURLToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Drivers_DriverId1",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_DriverId1",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DriverId1",
                table: "Trips");

            migrationBuilder.AddColumn<string>(
                name: "ImageURL",
                table: "Trips",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageURL",
                table: "Trips");

            migrationBuilder.AddColumn<int>(
                name: "DriverId1",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DriverId1",
                table: "Trips",
                column: "DriverId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Drivers_DriverId1",
                table: "Trips",
                column: "DriverId1",
                principalTable: "Drivers",
                principalColumn: "DriverId");
        }
    }
}
