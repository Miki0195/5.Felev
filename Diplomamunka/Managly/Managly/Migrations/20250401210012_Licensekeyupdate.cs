using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Managly.Migrations
{
    /// <inheritdoc />
    public partial class Licensekeyupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "LicenseKeys",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "LicenseKeys");
        }
    }
}
