using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentReceiptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptContentType",
                table: "InvoicePayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFileName",
                table: "InvoicePayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptPath",
                table: "InvoicePayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReceiptSize",
                table: "InvoicePayments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptUploadedAt",
                table: "InvoicePayments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptContentType",
                table: "InvoicePayments");

            migrationBuilder.DropColumn(
                name: "ReceiptFileName",
                table: "InvoicePayments");

            migrationBuilder.DropColumn(
                name: "ReceiptPath",
                table: "InvoicePayments");

            migrationBuilder.DropColumn(
                name: "ReceiptSize",
                table: "InvoicePayments");

            migrationBuilder.DropColumn(
                name: "ReceiptUploadedAt",
                table: "InvoicePayments");
        }
    }
}
