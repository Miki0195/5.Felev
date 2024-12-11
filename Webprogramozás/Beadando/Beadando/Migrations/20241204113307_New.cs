using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Beadando.Migrations
{
    /// <inheritdoc />
    public partial class New : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "LaLiga");

            migrationBuilder.UpdateData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Premier League");

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FinalScore", "HalfTimeScore", "Report", "StartTime" },
                values: new object[] { "3:2", "2:1", "Barcelona wins a thrilling El Clasico!", new DateTime(2024, 12, 4, 18, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FinalScore", "HalfTimeScore", "Report", "StartTime" },
                values: new object[] { "1:1", "0:1", "Atletico Madrid equalizes late in the game.", new DateTime(2024, 12, 5, 18, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FinalScore", "HalfTimeScore", "LeagueId", "Report", "StartTime" },
                values: new object[] { "0:2", "0:1", 1, "Real Betis dominates the game.", new DateTime(2024, 12, 6, 18, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "FinalScore", "HalfTimeScore", "LeagueId", "Report", "StartTime" },
                values: new object[] { "2:2", "1:0", 1, "A balanced game ends in a draw.", new DateTime(2024, 12, 7, 18, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AwayTeamId", "FinalScore", "HalfTimeScore", "HomeTeamId", "Report", "StartTime" },
                values: new object[] { 10, "1:3", "0:2", 9, "Celta Vigo shocks Valencia with a strong performance.", new DateTime(2024, 12, 8, 18, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "Matches",
                columns: new[] { "Id", "AwayTeamId", "FinalScore", "HalfTimeScore", "HomeTeamId", "LeagueId", "Report", "StartTime" },
                values: new object[] { 11, 3, "1:1", "1:0", 1, 1, "Barcelona and Atletico Madrid settle for a draw.", new DateTime(2024, 12, 14, 18, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Barcelona");

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Atletico Madrid");

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Sevilla");

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "LeagueId", "Name" },
                values: new object[] { 1, "Real Sociedad" });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "LeagueId", "Name" },
                values: new object[] { 1, "Real Betis" });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "LeagueId", "Name" },
                values: new object[] { 1, "Villarreal" });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "LeagueId", "Name" },
                values: new object[] { 1, "Athletic Club" });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Valencia");

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "Celta Vigo");

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "LeagueId", "Name" },
                values: new object[,]
                {
                    { 11, 1, "Osasuna" },
                    { 12, 1, "Rayo Vallecano" },
                    { 13, 1, "Espanyol" },
                    { 14, 1, "Mallorca" },
                    { 15, 1, "Almeria" },
                    { 16, 1, "Girona" },
                    { 17, 1, "Cadiz" },
                    { 18, 1, "Getafe" },
                    { 19, 1, "Real Valladolid" },
                    { 20, 1, "Elche" },
                    { 21, 2, "Arsenal" },
                    { 22, 2, "Manchester City" },
                    { 23, 2, "Manchester United" },
                    { 24, 2, "Liverpool" },
                    { 25, 2, "Chelsea" },
                    { 26, 2, "Tottenham Hotspur" },
                    { 27, 2, "Newcastle United" },
                    { 28, 2, "Brighton & Hove Albion" },
                    { 29, 2, "Brentford" },
                    { 30, 2, "Aston Villa" },
                    { 31, 2, "Fulham" },
                    { 32, 2, "Crystal Palace" },
                    { 33, 2, "Wolverhampton Wanderers" },
                    { 34, 2, "West Ham United" },
                    { 35, 2, "Leeds United" },
                    { 36, 2, "Leicester City" },
                    { 37, 2, "Everton" },
                    { 38, 2, "Southampton" },
                    { 39, 2, "Nottingham Forest" },
                    { 40, 2, "Bournemouth" }
                });

            migrationBuilder.InsertData(
                table: "Matches",
                columns: new[] { "Id", "AwayTeamId", "FinalScore", "HalfTimeScore", "HomeTeamId", "LeagueId", "Report", "StartTime" },
                values: new object[,]
                {
                    { 6, 22, "2:0", "1:0", 21, 2, "Arsenal outclasses Manchester City.", new DateTime(2024, 12, 9, 18, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 7, 24, "1:1", "0:0", 23, 2, "A tense derby ends in a draw.", new DateTime(2024, 12, 10, 18, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 8, 26, "3:1", "2:0", 25, 2, "Chelsea secures a convincing win over Spurs.", new DateTime(2024, 12, 11, 18, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 9, 28, "0:1", "0:0", 27, 2, "Brighton edges out Newcastle with a late goal.", new DateTime(2024, 12, 12, 18, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 10, 30, "2:2", "1:2", 29, 2, "Aston Villa and Brentford share points in a thrilling match.", new DateTime(2024, 12, 13, 18, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 12, 23, "2:2", "1:2", 22, 2, "The Manchester derby ends in a dramatic draw.", new DateTime(2024, 12, 15, 18, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.UpdateData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Football");

            migrationBuilder.UpdateData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Basketball");

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FinalScore", "HalfTimeScore", "Report", "StartTime" },
                values: new object[] { "2:1", "1:1", "Manchester United secured a victory in the final minutes!", new DateTime(2023, 11, 1, 18, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FinalScore", "HalfTimeScore", "Report", "StartTime" },
                values: new object[] { "3:2", "2:1", "Liverpool edged out Barcelona in a thrilling game.", new DateTime(2023, 11, 3, 20, 30, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FinalScore", "HalfTimeScore", "LeagueId", "Report", "StartTime" },
                values: new object[] { "112:108", "55:50", 2, "LeBron James led the Lakers to a narrow victory over the Warriors.", new DateTime(2023, 11, 5, 19, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "FinalScore", "HalfTimeScore", "LeagueId", "Report", "StartTime" },
                values: new object[] { "98:95", "45:50", 2, "The Bulls triumphed in a tightly contested match.", new DateTime(2023, 11, 6, 21, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Matches",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AwayTeamId", "FinalScore", "HalfTimeScore", "HomeTeamId", "Report", "StartTime" },
                values: new object[] { 3, "4:0", "1:0", 4, "Epic victory for Barcelona", new DateTime(2024, 11, 21, 19, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Manchester United");

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Liverpool");

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Barcelona");

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "LeagueId", "Name" },
                values: new object[] { 2, "Los Angeles Lakers" });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "LeagueId", "Name" },
                values: new object[] { 2, "Golden State Warriors" });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "LeagueId", "Name" },
                values: new object[] { 2, "Chicago Bulls" });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "LeagueId", "Name" },
                values: new object[] { 2, "Miami Heat" });

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Manchester City");

            migrationBuilder.UpdateData(
                table: "Teams",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "Athletico Madrid");
        }
    }
}
