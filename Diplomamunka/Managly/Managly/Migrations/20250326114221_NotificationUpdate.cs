using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Managly.Migrations
{
    /// <inheritdoc />
    public partial class NotificationUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetaData",
                table: "Notifications",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedUserId",
                table: "Notifications",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TaskId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedByUserId",
                table: "AttendanceAuditLogs",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_AttendanceId",
                table: "AttendanceAuditLogs",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_ModifiedByUserId",
                table: "AttendanceAuditLogs",
                column: "ModifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceAuditLogs_AspNetUsers_ModifiedByUserId",
                table: "AttendanceAuditLogs",
                column: "ModifiedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceAuditLogs_Attendances_AttendanceId",
                table: "AttendanceAuditLogs",
                column: "AttendanceId",
                principalTable: "Attendances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceAuditLogs_AspNetUsers_ModifiedByUserId",
                table: "AttendanceAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceAuditLogs_Attendances_AttendanceId",
                table: "AttendanceAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceAuditLogs_AttendanceId",
                table: "AttendanceAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceAuditLogs_ModifiedByUserId",
                table: "AttendanceAuditLogs");

            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RelatedUserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedByUserId",
                table: "AttendanceAuditLogs",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
