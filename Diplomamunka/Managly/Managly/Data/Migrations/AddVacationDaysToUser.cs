using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Managly.Data.Migrations
{
    public partial class AddVacationDaysToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add vacation days fields to AspNetUsers table
            migrationBuilder.AddColumn<int>(
                name: "TotalVacationDays",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<int>(
                name: "UsedVacationDays",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VacationYear",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: DateTime.Now.Year);

            // Add WorkingDaysCount to LeaveRequests table
            migrationBuilder.AddColumn<int>(
                name: "WorkingDaysCount",
                table: "LeaveRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove vacation days fields from AspNetUsers table
            migrationBuilder.DropColumn(
                name: "TotalVacationDays",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UsedVacationDays",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VacationYear",
                table: "AspNetUsers");

            // Remove WorkingDaysCount from LeaveRequests table
            migrationBuilder.DropColumn(
                name: "WorkingDaysCount",
                table: "LeaveRequests");
        }
    }
} 