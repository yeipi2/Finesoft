using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailPreferenceContentOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeClients",
                table: "ReportEmailPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeDashboard",
                table: "ReportEmailPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeEmployees",
                table: "ReportEmailPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeFinancial",
                table: "ReportEmailPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludePerformance",
                table: "ReportEmailPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeProjects",
                table: "ReportEmailPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeClients",
                table: "ReportEmailPreferences");

            migrationBuilder.DropColumn(
                name: "IncludeDashboard",
                table: "ReportEmailPreferences");

            migrationBuilder.DropColumn(
                name: "IncludeEmployees",
                table: "ReportEmailPreferences");

            migrationBuilder.DropColumn(
                name: "IncludeFinancial",
                table: "ReportEmailPreferences");

            migrationBuilder.DropColumn(
                name: "IncludePerformance",
                table: "ReportEmailPreferences");

            migrationBuilder.DropColumn(
                name: "IncludeProjects",
                table: "ReportEmailPreferences");
        }
    }
}
