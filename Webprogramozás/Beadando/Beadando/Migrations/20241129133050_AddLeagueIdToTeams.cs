using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beadando.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueIdToTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Disable foreign key checks
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");

            // Add LeagueId column with default value (e.g., 1 for "Football")
            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Teams",
                nullable: false,
                defaultValue: 1); // Adjust default value as needed

            // Create foreign key after ensuring valid LeagueId values
            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Leagues_LeagueId",
                table: "Teams",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Re-enable foreign key checks
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");

            migrationBuilder.DropForeignKey(name: "FK_Teams_Leagues_LeagueId", table: "Teams");
            migrationBuilder.DropColumn(name: "LeagueId", table: "Teams");

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }

    }
}
