using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppsettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmtpServer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoreName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StorePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StoreAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReceiptFooter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PrinterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrintReceiptAfterSave = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            
               

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "Port", "PrintReceiptAfterSave", "PrinterName", "ReceiptFooter", "SenderEmail", "SenderPassword", "SmtpServer", "StoreAddress", "StoreName", "StorePhone" },
                values: new object[] { 1, 587, true, "Default", "شكراً لزيارتكم", "Example@gmail.com", "hpufksdgfsbkmejc", "smtp.gmail.com", "Address", "Venice Sweets", "0780000000" });

            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            
        }
    }
}
