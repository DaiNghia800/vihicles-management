using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Public_Transport.Migrations
{
    /// <inheritdoc />
    public partial class AddImgForDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImage",
                table: "Drivers",
                type: "nvarchar(max)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImage",
                table: "Drivers");
        }
    }
}
