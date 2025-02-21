using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Managly.Migrations
{
    /// <inheritdoc />
    public partial class MessageModelRollBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Messages_RepliedToMessageId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_RepliedToMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "RepliedToMessageId",
                table: "Messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepliedToMessageId",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RepliedToMessageId",
                table: "Messages",
                column: "RepliedToMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Messages_RepliedToMessageId",
                table: "Messages",
                column: "RepliedToMessageId",
                principalTable: "Messages",
                principalColumn: "Id");
        }
    }
}
