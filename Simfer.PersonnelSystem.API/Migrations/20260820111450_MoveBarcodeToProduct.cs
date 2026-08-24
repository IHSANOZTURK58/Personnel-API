using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simfer.PersonnelSystem.API.Migrations
{
    public partial class MoveBarcodeToProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BarcodeNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "BarcodeNumber",
                table: "FaultyProducts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BarcodeNumber",
                table: "FaultyProducts");

            migrationBuilder.AddColumn<string>(
                name: "BarcodeNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
