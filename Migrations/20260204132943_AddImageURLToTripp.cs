using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Public_Transport.Migrations
{
    /// <inheritdoc />
    public partial class AddImageURLToTripp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageURL",
                table: "Trips",
                newName: "Thumbnail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Thumbnail",
                table: "Trips",
                newName: "ImageURL");
        }
    }
}
