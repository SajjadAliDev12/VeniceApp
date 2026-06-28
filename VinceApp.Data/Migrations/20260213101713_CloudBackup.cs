using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CloudBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<bool>(
                name: "AutoCloudBackup",
                table: "AppSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BackupFrequencyHours",
                table: "AppSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CloudBackupProvider",
                table: "AppSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudRefreshToken",
                table: "AppSettings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudUserEmail",
                table: "AppSettings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoCloudBackup",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "BackupFrequencyHours",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "CloudBackupProvider",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "CloudRefreshToken",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "CloudUserEmail",
                table: "AppSettings");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
