using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class changeordertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isPaid",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isReady",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isSentToKitchen",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isServed",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isPaid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "isReady",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "isSentToKitchen",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "isServed",
                table: "Orders");
        }
    }
}
