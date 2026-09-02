using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simfer.PersonnelSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddResolvedByColumnV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResolvedByUserId",
                table: "FaultyProducts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaultyProducts_ResolvedByUserId",
                table: "FaultyProducts",
                column: "ResolvedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaultyProducts_Users_ResolvedByUserId",
                table: "FaultyProducts",
                column: "ResolvedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaultyProducts_Users_ResolvedByUserId",
                table: "FaultyProducts");

            migrationBuilder.DropIndex(
                name: "IX_FaultyProducts_ResolvedByUserId",
                table: "FaultyProducts");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "FaultyProducts");
        }
    }
}
