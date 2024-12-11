using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beadando.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteLeagueToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FavoriteLeagueId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoriteLeagueId",
                table: "AspNetUsers");
        }
    }
}
