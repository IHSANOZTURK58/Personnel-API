using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simfer.PersonnelSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserRelationFromFaultCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaultCategories_Users_ResolvedByUserId",
                table: "FaultCategories");

            migrationBuilder.DropIndex(
                name: "IX_FaultCategories_ResolvedByUserId",
                table: "FaultCategories");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "FaultCategories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResolvedByUserId",
                table: "FaultCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaultCategories_ResolvedByUserId",
                table: "FaultCategories",
                column: "ResolvedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaultCategories_Users_ResolvedByUserId",
                table: "FaultCategories",
                column: "ResolvedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
