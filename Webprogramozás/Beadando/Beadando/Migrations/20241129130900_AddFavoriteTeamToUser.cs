using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beadando.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteTeamToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FavoriteTeamId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoriteTeamId",
                table: "AspNetUsers");
        }
    }
}
