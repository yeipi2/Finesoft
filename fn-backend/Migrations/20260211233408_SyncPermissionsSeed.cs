using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class SyncPermissionsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "QuoteItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_TicketId",
                table: "QuoteItems",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteItems_Tickets_TicketId",
                table: "QuoteItems",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteItems_Tickets_TicketId",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_TicketId",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Projects");
        }
    }
}
