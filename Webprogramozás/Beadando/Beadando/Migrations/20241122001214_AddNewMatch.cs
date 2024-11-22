using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beadando.Migrations
{
    /// <inheritdoc />
    public partial class AddNewMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Matches",
                columns: new[] { "Id", "AwayTeamId", "FinalScore", "HalfTimeScore", "HomeTeamId", "LeagueId", "Report", "StartTime" },
                values: new object[] { 5, 3, "4:0", "1:0", 4, 1, "Epic victory for Barcelona", new DateTime(2024, 11, 21, 19, 0, 0, 0, DateTimeKind.Unspecified) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
