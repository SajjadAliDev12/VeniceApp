using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderStatus",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderStatus",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "Open");
        }
    }
}
