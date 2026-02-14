using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingFrequencyToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingFrequency",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingFrequency",
                table: "Clients");
        }
    }
}
