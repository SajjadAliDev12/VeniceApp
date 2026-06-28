using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIceCreamPrinter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Printer",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Printer",
                table: "Categories");
        }
    }
}
