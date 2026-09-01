using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simfer.PersonnelSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class EstablishTableRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaultCategory",
                table: "FaultyProducts");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "FaultyProducts");

            migrationBuilder.AddColumn<int>(
                name: "FaultCategoryId",
                table: "FaultyProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "FaultyProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FaultyProducts_FaultCategoryId",
                table: "FaultyProducts",
                column: "FaultCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FaultyProducts_ProductId",
                table: "FaultyProducts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaultyProducts_FaultCategories_FaultCategoryId",
                table: "FaultyProducts",
                column: "FaultCategoryId",
                principalTable: "FaultCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FaultyProducts_Products_ProductId",
                table: "FaultyProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaultyProducts_FaultCategories_FaultCategoryId",
                table: "FaultyProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_FaultyProducts_Products_ProductId",
                table: "FaultyProducts");

            migrationBuilder.DropIndex(
                name: "IX_FaultyProducts_FaultCategoryId",
                table: "FaultyProducts");

            migrationBuilder.DropIndex(
                name: "IX_FaultyProducts_ProductId",
                table: "FaultyProducts");

            migrationBuilder.DropColumn(
                name: "FaultCategoryId",
                table: "FaultyProducts");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "FaultyProducts");

            migrationBuilder.AddColumn<string>(
                name: "FaultCategory",
                table: "FaultyProducts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "FaultyProducts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
