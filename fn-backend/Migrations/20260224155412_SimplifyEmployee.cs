using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CURP",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "RFC",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Salary",
                table: "Employees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CURP",
                table: "Employees",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RFC",
                table: "Employees",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Salary",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
