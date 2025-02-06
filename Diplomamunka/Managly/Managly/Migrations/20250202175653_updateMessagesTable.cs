using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Managly.Migrations
{
    /// <inheritdoc />
    public partial class updateMessagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentDate",
                table: "Messages",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "MessageContent",
                table: "Messages",
                newName: "Content");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "Messages",
                newName: "SentDate");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "Messages",
                newName: "MessageContent");
        }
    }
}
