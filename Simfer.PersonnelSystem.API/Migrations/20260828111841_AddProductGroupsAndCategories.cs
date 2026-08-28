using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simfer.PersonnelSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProductGroupsAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM UserHistories WHERE UserId NOT IN (SELECT Id FROM Users);");
            migrationBuilder.AddColumn<string>(
                name: "FaultCategory",
                table: "FaultyProducts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FaultCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaultCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserHistories_UserId",
                table: "UserHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserHistories_Users_UserId",
                table: "UserHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserHistories_Users_UserId",
                table: "UserHistories");

            migrationBuilder.DropTable(
                name: "FaultCategories");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropIndex(
                name: "IX_UserHistories_UserId",
                table: "UserHistories");

            migrationBuilder.DropColumn(
                name: "FaultCategory",
                table: "FaultyProducts");
        }
    }
}
