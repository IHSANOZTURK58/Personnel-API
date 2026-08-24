using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simfer.PersonnelSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToFaultyProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "FaultyProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FaultyProducts_UserId",
                table: "FaultyProducts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaultyProducts_Users_UserId",
                table: "FaultyProducts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaultyProducts_Users_UserId",
                table: "FaultyProducts");

            migrationBuilder.DropIndex(
                name: "IX_FaultyProducts_UserId",
                table: "FaultyProducts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FaultyProducts");
        }
    }
}
