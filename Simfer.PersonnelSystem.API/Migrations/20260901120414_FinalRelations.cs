using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simfer.PersonnelSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class FinalRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaultyProducts_FaultCategories_FaultCategoryId",
                table: "FaultyProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_FaultyProducts_Products_ProductId",
                table: "FaultyProducts");

            migrationBuilder.AddForeignKey(
                name: "FK_FaultyProducts_FaultCategories_FaultCategoryId",
                table: "FaultyProducts",
                column: "FaultCategoryId",
                principalTable: "FaultCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FaultyProducts_Products_ProductId",
                table: "FaultyProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
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
    }
}
