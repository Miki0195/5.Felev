using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Managly.Migrations
{
    /// <inheritdoc />
    public partial class AddedProjectToScedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ProjectId",
                table: "Schedules",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Projects_ProjectId",
                table: "Schedules",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Projects_ProjectId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_ProjectId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Schedules");
        }
    }
}
