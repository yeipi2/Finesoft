using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceCancellationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancelledDate",
                table: "Invoices");
        }
    }
}
